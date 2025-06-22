#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Copy workflow files from workflows-temp/ to their proper GitHub locations
    
.DESCRIPTION
    Due to GitHub Apps limitations in updating workflow files, this script manually copies
    workflow files from the temporary location to their proper GitHub directories.
    
.PARAMETER DryRun
    Show what would be copied without actually copying files
    
.EXAMPLE
    ./copy-workflows.ps1
    Copy all workflow files to their proper locations
    
.EXAMPLE  
    ./copy-workflows.ps1 -DryRun
    Show what would be copied without making changes
#>

param(
    [switch]$DryRun
)

# Ensure we're in the correct directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Push-Location $scriptDir

try {
    Write-Host "🔄 Copying workflow files from workflows-temp/ to proper GitHub locations..." -ForegroundColor Cyan
    
    # Define source and destination mappings
    $fileMappings = @(
        @{
            Source = "workflows-temp/sync-configurable-files.yml"
            Destination = ".github/workflows/sync-configurable-files.yml"
            Description = "Sync workflow YAML file"
        },
        @{
            Source = "workflows-temp/sync-config.yml" 
            Destination = ".github/sync-config.yml"
            Description = "Sync configuration file"
        },
        @{
            Source = "workflows-temp/sync-configurable-files.ps1"
            Destination = ".github/scripts/sync-configurable-files.ps1"
            Description = "Sync PowerShell script"
        }
    )
    
    # Check if workflows-temp directory exists
    if (-not (Test-Path "workflows-temp")) {
        Write-Error "❌ workflows-temp/ directory not found. Please generate workflow files first."
        exit 1
    }
    
    # Create destination directories if they don't exist
    $directories = @(".github/workflows", ".github/scripts")
    foreach ($dir in $directories) {
        if (-not (Test-Path $dir)) {
            if ($DryRun) {
                Write-Host "📁 Would create directory: $dir" -ForegroundColor Yellow
            } else {
                New-Item -ItemType Directory -Path $dir -Force | Out-Null
                Write-Host "📁 Created directory: $dir" -ForegroundColor Green
            }
        }
    }
    
    # Copy each file
    $copiedCount = 0
    $skippedCount = 0
    
    foreach ($mapping in $fileMappings) {
        $sourcePath = $mapping.Source
        $destPath = $mapping.Destination
        $description = $mapping.Description
        
        if (Test-Path $sourcePath) {
            if ($DryRun) {
                Write-Host "📄 Would copy: $sourcePath → $destPath ($description)" -ForegroundColor Yellow
            } else {
                Copy-Item -Path $sourcePath -Destination $destPath -Force
                Write-Host "✅ Copied: $sourcePath → $destPath ($description)" -ForegroundColor Green
                $copiedCount++
            }
        } else {
            Write-Host "⚠️  Skipped: $sourcePath (file not found)" -ForegroundColor Yellow
            $skippedCount++
        }
    }
    
    # Summary
    Write-Host ""
    if ($DryRun) {
        Write-Host "🔍 Dry run completed. No files were actually copied." -ForegroundColor Cyan
    } else {
        Write-Host "✨ Copy operation completed!" -ForegroundColor Green
        Write-Host "   Copied: $copiedCount files" -ForegroundColor Green
        if ($skippedCount -gt 0) {
            Write-Host "   Skipped: $skippedCount files (not found)" -ForegroundColor Yellow
        }
    }
    
    # Next steps guidance
    Write-Host ""
    Write-Host "📋 Next Steps:" -ForegroundColor Cyan
    Write-Host "   1. Review the copied workflow files in .github/" -ForegroundColor White
    Write-Host "   2. Commit and push the changes" -ForegroundColor White
    Write-Host "   3. Test the sync workflow via manual trigger in GitHub Actions" -ForegroundColor White
    Write-Host "   4. Configure repository secrets if needed (PAT_TOKEN, etc.)" -ForegroundColor White
    
} catch {
    Write-Error "❌ Error during copy operation: $_"
    exit 1
} finally {
    Pop-Location
}