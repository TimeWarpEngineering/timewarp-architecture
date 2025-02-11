# 001_Add-Aspire-Integration.md

## Description

Add .NET Aspire to enhance local debugging and monitoring capabilities, providing better visibility into the application's behavior during development.

## Requirements

- Integrate .NET Aspire into the existing solution
- Configure services for local development and monitoring
- Ensure all services can be launched and debugged through Aspire

## Checklist

### Implementation
- [x] Create new Aspire AppHost project
- [x] Add Aspire.Hosting NuGet package
- [x] Create ServiceDefaults project for shared configuration
- [x] Add Aspire service packages to ServiceDefaults
- [x] Reference ServiceDefaults in API and Web projects
- [x] Add project references in AppHost
- [x] Configure service endpoints
- [x] Test local dashboard and debugging

### Documentation
- [x] Update development setup guide
- [x] Document local debugging procedures

## Notes

Reference: [Add .NET Aspire to an existing .NET application](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/add-aspire-existing-app?tabs=windows&pivots=dotnet-cli)

Implementation Steps:
1. Create AppHost project:
   ```
   dotnet new aspire-apphost -o PdrMobile.AppHost
   ```
2. Create ServiceDefaults project:
   ```
   dotnet new aspire-servicedefaults -o PdrMobile.ServiceDefaults
   ```
3. Add projects to AppHost:
   - PdrMobile.Api.Server ✓
   - PdrMobile.Web.Server ✓
4. Configure service endpoints and dependencies ✓
5. Update solution file to include AppHost project ✓
6. Test with `dotnet run --project PdrMobile.AppHost` ✓

## Implementation Notes

- Created AppHost project successfully
- Fixed package versioning:
  - Removed version from PackageReference in AppHost project
  - Added Aspire.Hosting.AppHost version to Directory.Packages.props
  - Project uses central package version management
- Created ServiceDefaults project:
  - Added required Aspire service packages to central package management
  - Includes OpenTelemetry, health checks, and service discovery
- Added ServiceDefaults references to:
  - PdrMobile.Api.Server
  - PdrMobile.Web.Server
- Added AddServiceDefaults() to both servers' Program.cs
- Added project references in AppHost for both servers
- Configured services in Program.cs with proper dependencies
- Testing completed successfully:
  - Aspire dashboard launched at https://localhost:17256
  - Both api-service and web-service show "Running" state
  - Services properly configured with endpoints
  - Dashboard shows proper service dependencies
- Documentation:
  - Created comprehensive guide in Documentation/Developer/HowToGuides/AspireDevelopment.md
  - Includes setup instructions, configuration details, and debugging procedures
  - Added monitoring and troubleshooting sections
  - Referenced official documentation

Task Status: Complete ✓
