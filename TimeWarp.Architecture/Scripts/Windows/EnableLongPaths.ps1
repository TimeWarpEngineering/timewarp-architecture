# Check if the OS is Windows
if ($env:OS -eq "Windows_NT") {
  # Enable long paths in Windows
  New-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem" -Name "LongPathsEnabled" -Value 1 -PropertyType DWORD -Force | Out-Null
  Write-Host "Long paths have been enabled in Windows."
} else {
  Write-Host "This script is only needed on Windows systems."
}

# Check if Git is installed
if (Get-Command git -ErrorAction SilentlyContinue) {
  # Enable long paths in Git
  git config --global core.longpaths true
  Write-Host "Long paths have been enabled in Git."
} else {
  Write-Host "Git is not installed. Please install Git to enable long paths in Git."
}
