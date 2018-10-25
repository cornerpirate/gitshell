# gitshell
A PoC .net shell which uses a GitHub.com repository for the communication channel.

# Gasp at it in action!

Showing a victim executing a shell and attacker executing commands:

![ExampleShell](images/ExampleShell.png?raw=true "Example Shell")

For more information on how it all hangs together please review the companion blog post from SecarmaLabs over here:

https://blog.secarma.co.uk/labs/git-shell-proof-of-concept

# GitShellVictim

Clone down the repository and then open "GitShellVictim.sln" inside Visual Studio 2017. You can then read the source or compile the binary yourself.

There is a version of the binary in the "Debug" folder already. However, why would you trust that a hacker like me has given you the actual Binary? I mean, I did... But... Read the code and compile your own. Maybe you don't want all commits to your repository to be by @cornerpirate? You can fix that in the code if you want to.

# GitShellAttacker

This folder contains the python script used on the attacker's side. This has only been tested on Kali and should work if you install the dependencies listed in Readme.md within this folder.

# Disclaimer

This is for research purposes only. Do not use this in any situation where you are unauthorised to do so.