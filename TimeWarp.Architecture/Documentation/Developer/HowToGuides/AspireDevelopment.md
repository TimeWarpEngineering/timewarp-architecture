# Using .NET Aspire for Local Development

## Overview

This guide explains how to use .NET Aspire for local development and debugging. Aspire provides enhanced monitoring and debugging capabilities through a centralized dashboard, making it easier to understand and troubleshoot the application's behavior during development.

## Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022 or VS Code

## Project Structure

The solution includes the following Aspire-related projects:

- `PdrMobile.AppHost`: The Aspire host project that orchestrates all services
- `PdrMobile.ServiceDefaults`: Shared configuration and dependencies for all services

## Running the Application with Aspire

1. Start the application using the AppHost project:
   ```powershell
   dotnet run --project PdrMobile.AppHost
   ```

2. The Aspire dashboard will launch automatically, providing a URL like:
   ```
   https://localhost:17256/login?t=<token>
   ```

3. Open the provided URL in your browser to access the Aspire dashboard.

## Dashboard Features

The Aspire dashboard provides:

- Service status monitoring
- Endpoint information for each service
- Health check status
- Logging integration
- Distributed tracing
- Service dependencies visualization

## Service Configuration

Services in the solution are configured to use Aspire's features through:

1. Project References:
   - Each service project references `PdrMobile.ServiceDefaults`
   - The AppHost project references all service projects

2. Service Registration:
   ```csharp
   // In PdrMobile.AppHost/Program.cs
   var builder = DistributedApplication.CreateBuilder(args);
   
   var apiService = builder.AddProject<Projects.PdrMobile_Api_Server>("api-service");
   var webService = builder.AddProject<Projects.PdrMobile_Web_Server>("web-service")
       .WithReference(apiService);
   ```

3. Service Defaults Integration:
   ```csharp
   // In each service's Program.cs
   builder.AddServiceDefaults();
   ```

## Monitoring and Debugging

1. **Service Health**:
   - View service status in the dashboard
   - Monitor health check endpoints
   - Track service dependencies

2. **Logging**:
   - Centralized log collection
   - Filter and search capabilities
   - Structured logging support

3. **Distributed Tracing**:
   - Track requests across services
   - Analyze performance bottlenecks
   - View detailed timing information

## Troubleshooting

Common issues and solutions:

1. **Service Not Starting**:
   - Check the service's logs in the dashboard
   - Verify port availability
   - Check service dependencies

2. **Dashboard Not Loading**:
   - Ensure the AppHost project is running
   - Check the provided dashboard URL
   - Verify network connectivity

## Best Practices

1. **Development Workflow**:
   - Use the Aspire dashboard as your primary monitoring tool
   - Keep the dashboard open while developing
   - Check service health before debugging issues

2. **Service Configuration**:
   - Use ServiceDefaults for shared configuration
   - Configure appropriate health checks
   - Enable relevant telemetry

## Additional Resources

- [Official .NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Adding Aspire to Existing Applications](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/add-aspire-existing-app)
