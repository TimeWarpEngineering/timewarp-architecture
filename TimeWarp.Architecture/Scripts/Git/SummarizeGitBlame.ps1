# Ensure you're on the PR branch
git checkout Cramer/2024-05-08/UserSearchAndDetails

# Get the list of files changed in the PR
$files = git diff --name-only main Cramer/2024-05-08/UserSearchAndDetails

# Initialize a hashtable to store line counts by author
$authorLines = @{}
$totalLines = 0

# Loop through each file and get the blame information
foreach ($file in $files) {
    if (Test-Path $file) {
        $blame = git blame --line-porcelain $file
        $blameLines = $blame -split "`n"
        foreach ($line in $blameLines) {
            if ($line -match "^author (.+)") {
                $author = $matches[1]
                if (-not $authorLines.ContainsKey($author)) {
                    $authorLines[$author] = 0
                }
                $authorLines[$author]++
                $totalLines++
            }
        }
    } else {
        Write-Output "Skipping file '$file' as it does not exist in the current branch."
    }
}

# Output the results
$authorLines.GetEnumerator() | ForEach-Object {
    $percentage = [math]::Round(($_.Value / $totalLines) * 100, 2)
    Write-Output "$($_.Key): $($_.Value) lines ($percentage%)"
}
