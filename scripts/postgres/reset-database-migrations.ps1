# reset-database-migrations.ps1
# Destructive: deletes committed migrations, scaffolds a fresh initial migration, updates DB.
# Prefer accreting migrations (task 147-007 D9) unless you intentionally squash.

param (
  [string]$NewMigrationName = "InitialPostgresModel"
)

Push-Location $PSScriptRoot

try {
  . .\ef-shared-variables.ps1

  if (Test-Path $migrationsFolderPath) {
    Write-Output "Deleting migrations folder: $migrationsFolderPath"
    Remove-Item $migrationsFolderPath -Recurse -Force
  }

  .\add-migration.ps1 -MigrationName $NewMigrationName
  .\update-database.ps1
}
finally {
  Pop-Location
}
