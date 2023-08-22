# Preventing Local Commits to the Master Branch

This guide explains how to prevent direct commits to the master branch locally. This can be useful to enforce a workflow where changes are made in feature branches and then merged into the master branch through pull requests.

## Using PowerShell (Cross-Platform)

PowerShell is available on Windows, macOS, and Linux, so this method can be used across different operating systems.

1. **Navigate to Your Repository**: Open PowerShell and navigate to the root directory of your Git repository.

2. **Create a Pre-Commit Hook**: Create a file named `pre-commit` inside the `.git/hooks` directory of your repository.

3. **Edit the Pre-Commit Hook**: Open the `pre-commit` file in a text editor and add the following script:

   /```
   #!/usr/bin/env powershell

   $branch = & git rev-parse --abbrev-ref HEAD

   if ($branch -eq "master") {
       Write-Host "You can't commit directly to the master branch."
       exit 1
   }
   /```

4. **Make the Hook Executable**: On Linux or macOS, run the following command:

   /```
   chmod +x .git/hooks/pre-commit
   /```

   On Windows, the file should be executable as-is.

## Conclusion

By following these steps, you can enforce a policy that prevents direct commits to the master branch locally. Please note that these are client-side enforcements and can be bypassed if someone intentionally modifies or deletes the hook. Server-side protections are recommended for more robust control.


## Conclusion

By following these steps, you can enforce a policy that prevents direct commits to the master branch locally on your machine. Please note that these are client-side enforcements and can be bypassed if someone intentionally modifies or deletes the hook.

For additional control and to enforce best practices within a team or organization, consider [Protecting this branch](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches) on GitHub. This server-side setting allows you to:

- Require pull request reviews before merging.
- Require status checks to pass before merging.
- Restrict who can push to the branch.
- Enforce other workflow policies as needed.

Protecting the master branch on GitHub ensures that the defined rules are followed when interacting with the remote repository, complementing the client-side controls described above.

To protect a branch on GitHub, go to the repository's settings, then click on "Branches," and choose "Add rule" for the branch you want to protect. Follow the prompts to configure the desired protections.

Combining client-side hooks with server-side branch protection provides a robust approach to maintaining a consistent and secure workflow.
