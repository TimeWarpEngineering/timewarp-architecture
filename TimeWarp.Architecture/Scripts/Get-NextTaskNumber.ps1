# Get-NextTaskNumber.ps1
#
# .SYNOPSIS
# Gets the next available task number for Kanban workflow management.
#
# .DESCRIPTION
# Scans all Kanban folders (Backlog, InProgress, ToDo, Done) for task files
# and determines the next available task number by finding the highest existing
# number and incrementing it by one.
#
# .PARAMETER KanbanPath
# Path to the Kanban directory. Defaults to the Kanban directory relative to the script location.
#
# .EXAMPLE
# Get-NextTaskNumber
# Returns: "004" (if highest existing task is 003)
#
# .EXAMPLE
# Get-NextTaskNumber -KanbanPath "C:\Projects\MyRepo\Kanban"
# Returns: Next available task number for the specified Kanban directory
#
# .NOTES
# Author: TimeWarp Engineering
# Last Modified: 2025-01-31
# Requires PowerShell 5.1 or later

function Get-NextTaskNumber {
  [CmdletBinding(SupportsShouldProcess = $true)]
  param(
    [Parameter(
      Position = 0,
      HelpMessage = "Path to the Kanban directory"
    )]
    [ValidateNotNullOrEmpty()]
    [ValidateScript({
      if (-not (Test-Path $_)) {
        throw "Kanban directory not found at: $_"
      }
      return $true
    })]
    [string]$KanbanPath = (Join-Path $PSScriptRoot "..\Kanban")
  )

  begin {
    # Constants
    $script:KanbanFolders = @("Backlog", "InProgress", "ToDo", "Done")
    $script:TaskNumberPattern = "^(\d{3})_"
    $script:HighestTaskNumber = 0

    Write-Verbose "Starting task number search in: $KanbanPath"
    Write-Verbose "Scanning folders: $($KanbanFolders -join ', ')"
  }

  process {
    try {
      foreach ($folderName in $KanbanFolders) {
        $currentFolderPath = Join-Path -Path $KanbanPath -ChildPath $folderName
        
        if (-not (Test-Path $currentFolderPath)) {
          Write-Warning "Folder not found: $currentFolderPath"
          continue
        }

        Write-Verbose "Processing folder: $folderName"

        # Get all markdown files and extract task numbers efficiently
        $taskFiles = Get-ChildItem -Path $currentFolderPath -Filter "*.md" -File
        
        if (-not $taskFiles) {
          Write-Verbose "No markdown files found in: $folderName"
          continue
        }

        $taskNumbers = $taskFiles | 
          Where-Object { $_.Name -match $TaskNumberPattern } |
          ForEach-Object { 
            Write-Verbose "Found task number: $($Matches[1]) in $($_.Name)"
            [int]$Matches[1] 
          }

        # Update highest number if any valid numbers were found
        if ($taskNumbers) {
          $folderMaxNumber = ($taskNumbers | Measure-Object -Maximum).Maximum
          $script:HighestTaskNumber = [Math]::Max($HighestTaskNumber, $folderMaxNumber)
          Write-Verbose "Current highest number: $HighestTaskNumber"
        }
      }

      # Calculate and format next task number
      $nextTaskNumber = $HighestTaskNumber + 1
      $formattedNumber = $nextTaskNumber.ToString("000")
      
      Write-Verbose "Generated next task number: $formattedNumber"
      return $formattedNumber
    }
    catch {
      $errorMessage = "Failed to process task numbers: $($_.Exception.Message)"
      Write-Error $errorMessage
      throw $errorMessage
    }
  }
}

# Run the function if script is executed directly
if ($MyInvocation.InvocationName -eq '.') {
  Get-NextTaskNumber
}
