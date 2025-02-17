# 011_Create-TimeWarp-Automation-Library.md

## Description

Create TimeWarp.Automation class library and its test project

## Requirements

```pwsh
# Create library
dotnet new classlib -n TimeWarp.Automation -o Source/Libraries/TimeWarp.Automation

# Create test project following:
# https://github.com/TimeWarpEngineering/timewarp-fixie
dotnet new classlib -n TimeWarp.Automation.Tests -o Tests/Libraries/TimeWarp.Automation.Tests
dotnet add Tests/Libraries/TimeWarp.Automation.Tests/TimeWarp.Automation.Tests.csproj package Fixie
dotnet add Tests/Libraries/TimeWarp.Automation.Tests/TimeWarp.Automation.Tests.csproj package Shouldly
