namespace Console_CSharp.Services;

using System;
using System.IO;
using System.Linq;

/// <summary>
/// Services related to git
/// </summary>
internal class GitService
{
  /// <summary>
  /// Get the root of the git repo if this is one.
  /// </summary>
  /// <returns>DirectoryInfo or null</returns>
  public DirectoryInfo GitRootDirectoryInfo()
  {
      var directory = new DirectoryInfo(Environment.CurrentDirectory);
      bool found = IsGitDirectory(directory);
      while (!found && directory.Parent != null)
      {
        directory = directory.Parent;
        found = IsGitDirectory(directory);
      }

      return directory;
    }

  /// <summary>
  /// Checks if the current directory is the root of a git repo.
  /// </summary>
  /// <param name="aDirectoryInfo"></param>
  /// <returns></returns>
  public bool IsGitDirectory(DirectoryInfo aDirectoryInfo)
  {
      const string GitDirectoryName = ".git";
      return aDirectoryInfo.GetDirectories(GitDirectoryName).Any();
    }
}