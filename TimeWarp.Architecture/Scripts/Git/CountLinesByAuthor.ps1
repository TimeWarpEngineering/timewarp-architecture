# Save the current directory
Push-Location

try {
  # Change to the root directory of the Git repository
  Set-Location (git rev-parse --show-toplevel)

  # Create an empty hashtable to store line counts by author
  $lineCounts = @{}

  # Get all files tracked by Git
  $files = git ls-tree -r HEAD --name-only

  # Check if files were fetched
  if ($files -eq $null) {
    Write-Host "No files found. Are you in a Git repository?"
    return
  }

  # Process each file
  foreach ($file in $files) {
    Write-Host "Processing file: $file"

    # Get blame for each file, extract author lines
    $blame = git blame --line-porcelain $file
    if ($blame -eq $null) {
      Write-Host "Unable to get blame for file: $file"
      continue
    }

    $authors = $blame | Select-String -Pattern "^author " | ForEach-Object { $_.Line.Substring(7) }

    # Count lines per author
    foreach ($author in $authors) {
      if ($lineCounts.ContainsKey($author)) {
        $lineCounts[$author] += 1
      } else {
        $lineCounts[$author] = 1
      }
    }
  }

  # Display the results
  if ($lineCounts.Count -eq 0) {
    Write-Host "No line counts to display. Check if the Git repository has any files."
  } else {
    $lineCounts.Keys | ForEach-Object {
      Write-Host "$_ : $($lineCounts[$_])"
    }
  }
} catch {
  # An error occurred
  Write-Host "An error occurred: $_"
} finally {
  # Return to the original directory
  Pop-Location
}
