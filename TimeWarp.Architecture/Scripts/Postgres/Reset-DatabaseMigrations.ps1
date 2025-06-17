# Reset-DatabaseMigrations.ps1

param (
    [string]$NewMigrationName = "InitialCreate"
    )
    
    # Get the script location and set project paths
    Push-Location $PSScriptRoot
    
try {
    . .\EfSharedVariables.ps1
    # Delete all migrations
    if (Test-Path $migrationsFolderPath) {
        Write-Output "Deleting migrations folder: $migrationsFolderPath"
        Remove-Item $migrationsFolderPath -Recurse -Force
    } 
 
    # Create a new migration
    .\Add-Migration.ps1 -migrationName $NewMigrationName
 
    # Update the database
    .\Update-Database.ps1
} 
finally {
    Pop-Location
}
