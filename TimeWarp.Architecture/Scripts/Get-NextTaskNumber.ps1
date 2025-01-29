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

function Get-NextTaskNumber {
  [CmdletBinding()]
  param(
    [Parameter(
      Position = 0,
      HelpMessage = "Path to the Kanban directory"
    )]
    [ValidateNotNullOrEmpty()]
    [string]$KanbanPath = (Join-Path $PSScriptRoot "..\Kanban")
  )

  begin {
    # Validate Kanban directory exists
    if (-not (Test-Path $KanbanPath)) {
      throw "Kanban directory not found at: $KanbanPath"
    }

    $Folders = @("Backlog", "InProgress", "ToDo", "Done")
    $HighestNumber = 0
    $TaskNumberPattern = "^(\d{3})_"
  }

  process {
    try {
      foreach ($Folder in $Folders) {
        $FolderPath = Join-Path -Path $KanbanPath -ChildPath $Folder
        
        if (Test-Path $FolderPath) {
          # Get all markdown files and extract task numbers in one operation
          $Numbers = Get-ChildItem -Path $FolderPath -Filter "*.md" |
            Where-Object { $_.Name -match $TaskNumberPattern } |
            ForEach-Object { [int]$Matches[1] }

          # Update highest number if any numbers were found
          if ($Numbers) {
            $FolderMax = ($Numbers | Measure-Object -Maximum).Maximum
            $HighestNumber = [Math]::Max($HighestNumber, $FolderMax)
          }
        }
      }

      $NextNumber = $HighestNumber + 1
      return $NextNumber.ToString("000")
    }
    catch {
      Write-Error "Error processing task numbers: $_"
      throw
    }
  }
}

# Export the function if being imported as a module
if ($MyInvocation.InvocationName -ne '.') {
  Export-ModuleMember -Function Get-NextTaskNumber
}
