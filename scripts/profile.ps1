# profile.ps1
Write-Host "Initializing repository environment..."

# Set useful aliases and functions for the repo
Set-Location $PSScriptRoot
$env:REPO_ROOT = Join-Path $PSScriptRoot -ChildPath ".."

Set-Location $env:REPO_ROOT
. scripts\get-next-task-number.ps1
Write-Host "Repository root: $env:REPO_ROOT"
Write-Host "Ready to work!"
