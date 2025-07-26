# Get the log and process it
$log = git log Cramer/2024-05-08/UserSearchAndDetails --not main --numstat --pretty=format:"%aN" | Out-String
$logLines = $log -split "`n"

$authorChanges = @{}

foreach ($line in $logLines) {
    if ($line -match "^\s*\d+\s+\d+\s+") {
        $parts = $line -split "\s+"
        $added = [int]$parts[0]
        $removed = [int]$parts[1]
        $authorChanges[$currentAuthor].Added += $added
        $authorChanges[$currentAuthor].Removed += $removed
    } elseif ($line -match "^\S") {
        $currentAuthor = $line.Trim()
        if (-not $authorChanges.ContainsKey($currentAuthor)) {
            $authorChanges[$currentAuthor] = [PSCustomObject]@{
                Added = 0
                Removed = 0
            }
        }
    }
}

# Output the summary
$authorChanges.GetEnumerator() | ForEach-Object {
    Write-Output "$($_.Key): +$($_.Value.Added) -$($_.Value.Removed)"
}
