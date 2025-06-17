# Preventing Local Commits to the Master Branch

This guide explains how to prevent direct commits to the master branch locally using a pre-commit hook. This can be useful to enforce a workflow where changes are made in feature branches and then merged into the master branch through pull requests.

## Using PowerShell (Cross-Platform)

PowerShell is available on Windows, macOS, and Linux, so this method can be used across different operating systems.

### Step 1: Create the PowerShell Script

1. **Navigate to Your Repository**: Open a terminal or PowerShell window and navigate to the root directory of your Git repository.

2. **Create a PowerShell Script**: Create a file named `pre-commit.ps1` inside the `.git/hooks` directory of your repository.

3. **Edit the PowerShell Script**: Open the `pre-commit.ps1` file in a text editor and add the following script:

   /```
   $branch = & git rev-parse --abbrev-ref HEAD

   if ($branch -eq "master") {
       Write-Host "You can't commit directly to the master branch."
       exit 1
   }
   /```

### Step 2: Create a Bash Wrapper

1. **Create a Bash Script**: Create a file named `pre-commit` (without any extension) inside the `.git/hooks` directory of your repository.

2. **Edit the Bash Script**: Open the `pre-commit` file in a text editor and add the following script:

   /```
   #!/bin/sh
   exec pwsh -NoProfile -File .git/hooks/pre-commit.ps1
   /```

3. **Make the Bash Script Executable**: On Linux or macOS, run the following command:

   /```
   chmod +x .git/hooks/pre-commit
   /```

   On Windows, the file should be executable as-is.

## Conclusion

By following these steps, you can enforce a policy that prevents direct commits to the master branch locally on your machine. This client-side enforcement complements server-side controls, such as  [Protecting this branch](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches), to provide a robust approach to maintaining a consistent and secure workflow.

To protect a branch on GitHub, go to the repository's settings, then click on "Branches," and choose "Add rule" for the branch you want to protect. Follow the prompts to configure the desired protections.
