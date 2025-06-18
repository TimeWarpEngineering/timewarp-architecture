# PowerShell script to handle syncing configurable files from a parent repository
# This script is called by the GitHub workflow

param (
    [string]$ConfigFile = ".github/sync-config.yml",
    [string]$GithubOutputFile = $env:GITHUB_OUTPUT,
    [string]$GithubStepSummary = $env:GITHUB_STEP_SUMMARY,
    [string]$GithubWorkspace = $env:GITHUB_WORKSPACE,
    [string]$GithubToken = $env:GITHUB_TOKEN
)

# Validate required parameters
if (-not $GithubToken) {
    Write-Error "GitHub token is required"
    exit 1
}

if (-not $GithubWorkspace) {
    Write-Error "GitHub workspace path is required"
    exit 1
}

Write-Host "Loading configuration from $ConfigFile"

# Check if yq is installed, if not, install it based on the operating system
if (-not (Get-Command yq -ErrorAction SilentlyContinue)) {
    Write-Host "Installing yq for YAML parsing..."
    if ($IsWindows) {
        if (Get-Command winget -ErrorAction SilentlyContinue) {
            Write-Host "Using winget to install yq on Windows..."
            winget install -e --id MikeFarah.yq -h
            if (-not (Get-Command yq -ErrorAction SilentlyContinue)) {
                Write-Error "Failed to install yq using winget"
                exit 1
            }
        } else {
            Write-Error "winget not found on Windows. Please install winget to proceed with yq installation."
            exit 1
        }
    } elseif ($IsLinux) {
        $yqUrl = "https://github.com/mikefarah/yq/releases/latest/download/yq_linux_amd64"
        $yqPath = "/usr/local/bin/yq"
        Invoke-WebRequest -Uri $yqUrl -OutFile $yqPath -UseBasicParsing
        chmod +x $yqPath
        if (-not (Test-Path $yqPath)) {
            Write-Error "Failed to download yq for Linux"
            exit 1
        }
    } else {
        Write-Error "Unsupported operating system for yq installation"
        exit 1
    }
}

# Check if config file exists
if (-not (Test-Path $ConfigFile)) {
    Write-Error "Error: Configuration file $ConfigFile not found. It is required for this workflow."
    exit 1
}

# Read configuration from file
$parentRepo = & yq eval '.parent.repository' $ConfigFile
$parentBranch = & yq eval '.parent.branch' $ConfigFile

# Get sync files as comma-separated list
$syncFiles = & yq eval '.sync_files | join(",")' $ConfigFile

# Get cron schedule if available
$cronSchedule = & yq eval '.schedule.cron' $ConfigFile
if ($cronSchedule -and $cronSchedule -ne "null") {
    Add-Content -Path $GithubOutputFile -Value "cron_schedule=$cronSchedule"
    Write-Host "  Cron Schedule: $cronSchedule"
} else {
    Add-Content -Path $GithubOutputFile -Value "cron_schedule=0 9 * * 1"
    Write-Host "  Cron Schedule: Using default (0 9 * * 1)"
}

Add-Content -Path $GithubOutputFile -Value "parent_repo=$parentRepo"
Add-Content -Path $GithubOutputFile -Value "parent_branch=$parentBranch"
Add-Content -Path $GithubOutputFile -Value "files_to_sync=$syncFiles"

Write-Host "Configuration loaded:"
Write-Host "  Parent Repository: $parentRepo"
Write-Host "  Parent Branch: $parentBranch"
Write-Host "  Files to sync: $syncFiles"
if ($cronSchedule -and $cronSchedule -ne "null") {
    Write-Host "  Cron Schedule from config: $cronSchedule"
}

# Create temporary directory for parent repo
$tempDir = Join-Path -Path $env:TEMP -ChildPath "parent-repo"
if (-not (Test-Path $tempDir)) {
    New-Item -ItemType Directory -Path $tempDir | Out-Null
}

# Download files from parent repository
Set-Location -Path $tempDir
$files = $syncFiles -split ","
$downloadedFiles = @()
$failedFiles = @()

Write-Host "Downloading files from $parentRepo@$parentBranch..."

# Get prefix from config if available, default to "TimeWarp.Architecture"
$prefix = & yq eval '.parent.prefix // "TimeWarp.Architecture"' $ConfigFile

# Use parallel jobs for downloading files if possible
$jobs = @()
foreach ($file in $files) {
    $file = $file.Trim()
    Write-Host "Attempting to download: $file"
    
    $sourcePath = "$prefix/$file"
    Write-Host "Source path (with prefix): $sourcePath"
    
    $fileDir = Split-Path -Path $file -Parent
    if ($fileDir -and -not (Test-Path $fileDir)) {
        New-Item -ItemType Directory -Path $fileDir | Out-Null
    }
    
    $url = "https://api.github.com/repos/$parentRepo/contents/$sourcePath?ref=$parentBranch"
    $headers = @{
        "Authorization" = "token ***" # Mask token in logs
        "Accept" = "application/vnd.github.v3.raw"
    }
    
    $job = Start-Job -ScriptBlock {
        param ($url, $headers, $file)
        try {
            $response = Invoke-WebRequest -Uri $url -Headers $headers -OutFile $file -UseBasicParsing -ErrorAction Stop
            if (Test-Path $file -PathType Leaf) {
                return @{ File = $file; Success = $true }
            } else {
                Remove-Item -Path $file -ErrorAction SilentlyContinue
                return @{ File = $file; Success = $false; Error = "Downloaded empty file" }
            }
        } catch {
            Remove-Item -Path $file -ErrorAction SilentlyContinue
            return @{ File = $file; Success = $false; Error = $_.Exception.Response.StatusCode }
        }
    } -ArgumentList $url, $headers, $file
    
    $jobs += $job
}

# Wait for all jobs to complete and collect results
foreach ($job in $jobs) {
    $result = Receive-Job -Job $job -Wait
    if ($result.Success) {
        $downloadedFiles += $result.File
        Write-Host "✓ Successfully downloaded: $($result.File)"
    } else {
        $failedFiles += $result.File
        Write-Host "✗ Failed to download: $($result.File) (Error: $($result.Error))"
    }
    Remove-Job -Job $job
}

# Output results
$env:DOWNLOADED_FILES = $downloadedFiles -join " "
$env:FAILED_FILES = $failedFiles -join " "

if ($downloadedFiles.Count -eq 0) {
    Write-Host "No files were successfully downloaded"
    exit 1
}

# Compare and update files
$changesMade = $false
$changedFiles = @()

foreach ($file in $downloadedFiles) {
    if (Test-Path $file -PathType Leaf) {
        $targetFile = Join-Path -Path $GithubWorkspace -ChildPath $file
        
        $targetDir = Split-Path -Path $targetFile -Parent
        if (-not (Test-Path $targetDir)) {
            New-Item -ItemType Directory -Path $targetDir | Out-Null
        }
        
        # Use file hash comparison to avoid loading large files into memory
        $sourceHash = Get-FileHash -Path $file -Algorithm SHA256
        if (Test-Path $targetFile) {
            $targetHash = Get-FileHash -Path $targetFile -Algorithm SHA256
            if ($sourceHash.Hash -ne $targetHash.Hash) {
                Write-Host "Updating file: $file (hash mismatch)"
                Copy-Item -Path $file -Destination $targetFile -Force
                $changedFiles += $file
                $changesMade = $true
            } else {
                Write-Host "No changes needed for: $file"
            }
        } else {
            Write-Host "Updating file: $file (target does not exist)"
            Copy-Item -Path $file -Destination $targetFile -Force
            $changedFiles += $file
            $changesMade = $true
        }
    }
}

Add-Content -Path $GithubOutputFile -Value "changes_made=$changesMade"
$env:CHANGED_FILES = $changedFiles -join " "

if ($changesMade) {
    Write-Host "Files updated: $($changedFiles -join ' ')"
} else {
    Write-Host "No files needed updating"
}

# Output summary
Add-Content -Path $GithubStepSummary -Value "## Sync Summary"
Add-Content -Path $GithubStepSummary -Value "**Parent Repository:** $parentRepo"
Add-Content -Path $GithubStepSummary -Value "**Parent Branch:** $parentBranch"
Add-Content -Path $GithubStepSummary -Value "**Files Configured for Sync:** $syncFiles"

if ($downloadedFiles) {
    Add-Content -Path $GithubStepSummary -Value "**Successfully Downloaded:** $($downloadedFiles -join ' ')"
}

if ($failedFiles) {
    Add-Content -Path $GithubStepSummary -Value "**Failed to Download:** $($failedFiles -join ' ')"
}

if ($changesMade) {
    Add-Content -Path $GithubStepSummary -Value "**Files Updated:** $($changedFiles -join ' ')"
    Add-Content -Path $GithubStepSummary -Value "**Status:** ✅ Pull request created with updates"
} else {
    Add-Content -Path $GithubStepSummary -Value "**Status:** ✅ No updates needed - all files are current"
}
