# How To Get All Contents In Directory Recursively

This guide will demonstrate two methods for using PowerShell to get the contents of each file in a directory, recursively going through all subdirectories. This is particularly useful for feeding content into AI systems like OpenAI's GPT models, as it allows you to easily gather and process large amounts of text data from multiple files.

## Method 1: Display Contents on the Screen

Use the following PowerShell command:

```powershell
Get-ChildItem -Path "YourFolderPath" -Recurse -File | ForEach-Object { Write-Host "`n// $($_.FullName)`n"; Get-Content $_.FullName }
```

Replace "YourFolderPath" with the path to the directory you want to start from. This command will first get a list of all files in the specified folder and its subfolders using `Get-ChildItem` with the `-Recurse` and `-File` flags. Then, for each file, it will display the file's full name (path) and its content using `ForEach-Object`, `Write-Host`, and `Get-Content`.

## Method 2: Copy Contents to Clipboard

Create a PowerShell function called `Get-ContentRecursively`:

```powershell
function Get-ContentRecursively {
    param(
        [string]$Path = (Get-Location)
    )

    $allContents = Get-ChildItem -Path $Path -Recurse -File | ForEach-Object { "`n$($_.FullName)`n" + (Get-Content $_.FullName -Raw) }
    $allContents | Set-Clipboard
}
```

This function takes an optional `Path` parameter which defaults to the current directory (`Get-Location`). It gets the content of each file in the specified directory and its subdirectories, and then it combines the file's full name (path) and content. The `-Raw` flag is used to preserve the original line endings and formatting. Finally, the combined contents are copied to the clipboard using the `Set-Clipboard` cmdlet.

To use the function, simply call it:

```powershell
Get-ContentRecursively
```

Or, if you want to specify a custom path:

```powershell
Get-ContentRecursively -Path "YourFolderPath"
```

## Note

Keep in mind that both methods will output or copy a large amount of text if there are many files with large amounts of content. Be cautious when using these methods with very large directories, as they may consume a lot of memory or cause your console to become unresponsive.
