# Entity Framework Core PowerShell Scripts

This folder contains a set of PowerShell scripts to help manage Entity Framework Core migrations and database operations for your project.

## Scripts

### Add-Migration.ps1

Adds a new migration with the specified name.

Usage:

```
.\Add-Migration.ps1 -MigrationName "YourMigrationName"
```

### Drop-Database.ps1

Drops the database.

Usage:

```
.\Drop-Database.ps1
```

### EfSharedVariables.ps1

Defines shared variables used by other PowerShell scripts in this folder, including paths to the project and startup projects, the DbContext class name, and the output directory for migrations.

### Reset-DatabaseMigrations.ps1

Deletes all existing migrations, creates a new migration with the specified name (default is "InitialCreate"), and updates the database with the new migration.

Usage:

```
.\Reset-DatabaseMigrations.ps1
```
or
```
.\Reset-DatabaseMigrations.ps1 -NewMigrationName "YourNewMigrationName"
```

### Update-Database.ps1

Updates the database to the latest migration.

Usage:

```
.\Update-Database.ps1
```

## Usage

To use these scripts, simply run them from the PowerShell command prompt. Ensure you have the required .NET SDK and Entity Framework Core tools installed.

