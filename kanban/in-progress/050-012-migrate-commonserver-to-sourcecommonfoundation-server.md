# Migrate Common.Server to source/common/foundation-server/

## Parent
050-establish-root-directory-structure-source-tests-in-kebab-case

## Summary

Migrate Common.Server from `TimeWarp.Architecture/Source/Common/` to `source/common/foundation-server/` with kebab-case file and directory naming. This is a Level 3 dependency project that provides shared server-side functionality for all ContainerApps server projects.

## Rationale

- **Level 3 dependency** — blocks 4 server projects: Api.Server, Grpc.Server, Web.Server, Yarp
- **Shared server infrastructure** — base endpoints, CORS policies, Azure App Configuration, Scalar OpenAPI
- **Key dependency migrated** — foundation-infrastructure is in source/
- **Completes Common layer migration** — last of the Common.* projects

## Current State

```
TimeWarp.Architecture/Source/Common/Common.Server/
├── Common.Server.csproj                  ← References foundation-infrastructure
├── AssemblyMarker.cs                     ← namespace: TimeWarp.Architecture.Common.Server
├── GlobalUsings.cs                       ← 20 global usings (FastEndpoints, Azure, Scalar, etc.)
├── CommonServerModule.cs                 ← Server module configuration (Azure App Config, Scalar, FluentValidation)
├── IAspNetModule.cs                      ← AspNet module interface
├── IAspNetProgram.cs                     ← AspNet program interface
├── Base/
│   ├── BaseEndpoint.cs                   ← Base FastEndpoint class
│   └── BaseFastEndpoint.cs               ← FastEndpoint base implementation
├── CorsPolicy/
│   ├── CorsPolicy.cs                     ← CORS policy definitions
│   └── CorsPolicies/
│       ├── AnyPolicy.cs                  ← Allow any origin
│       └── ExamplePolicy.cs              ← Example CORS policy
└── Extensions/
    ├── MvcBuilderExtensions.cs           ← MVC builder extensions
    └── ServiceUriHelper.cs               ← Service URI helper
```

## Dependencies (already migrated)

| Project | Migrated To |
|---------|-------------|
| Common.Infrastructure | source/common/foundation-infrastructure/ |

## Target State

```
source/common/foundation-server/
├── foundation-server.csproj
├── assembly-marker.cs
├── global-usings.cs
├── common-server-module.cs
├── i-aspnet-module.cs
├── i-aspnet-program.cs
├── base/
│   ├── base-endpoint.cs
│   └── base-fast-endpoint.cs
├── cors-policy/
│   ├── cors-policy.cs
│   └── cors-policies/
│       ├── any-policy.cs
│       └── example-policy.cs
└── extensions/
    ├── mvc-builder-extensions.cs
    └── service-uri-helper.cs
```

## Skills

- csharp

## Checklist

### Phase 1: Create Directory Structure
- [ ] Create source/common/foundation-server/base/
- [ ] Create source/common/foundation-server/cors-policy/cors-policies/
- [ ] Create source/common/foundation-server/extensions/

### Phase 2: Migrate Files
- [ ] git mv Common.Server.csproj → foundation-server.csproj
- [ ] git mv AssemblyMarker.cs → assembly-marker.cs
- [ ] git mv GlobalUsings.cs → global-usings.cs
- [ ] git mv CommonServerModule.cs → common-server-module.cs
- [ ] git mv IAspNetModule.cs → i-aspnet-module.cs
- [ ] git mv IAspNetProgram.cs → i-aspnet-program.cs
- [ ] git mv Base/BaseEndpoint.cs → base/base-endpoint.cs
- [ ] git mv Base/BaseFastEndpoint.cs → base/base-fast-endpoint.cs
- [ ] git mv CorsPolicy/CorsPolicy.cs → cors-policy/cors-policy.cs
- [ ] git mv CorsPolicy/CorsPolicies/AnyPolicy.cs → cors-policy/cors-policies/any-policy.cs
- [ ] git mv CorsPolicy/CorsPolicies/ExamplePolicy.cs → cors-policy/cors-policies/example-policy.cs
- [ ] git mv Extensions/MvcBuilderExtensions.cs → extensions/mvc-builder-extensions.cs
- [ ] git mv Extensions/ServiceUriHelper.cs → extensions/service-uri-helper.cs

### Phase 3: Update Project File
- [ ] Update csproj to minimal format (remove redundant properties inherited from source/Directory.Build.props)
- [ ] Keep PackageReferences (Azure.Identity, FastEndpoints, TimeWarp.Mediator, Microsoft.Azure.AppConfiguration.AspNetCore, OneOf, Scalar.AspNetCore)
- [ ] Keep FrameworkReference for Microsoft.AspNetCore.App
- [ ] Update ProjectReference path to foundation-infrastructure (sibling from new location)

### Phase 4: Update Source Files
- [ ] Add `#pragma warning disable CA1040` / `#pragma warning restore CA1040` to assembly-marker.cs
- [ ] Verify namespaces unchanged
  - AssemblyMarker: TimeWarp.Architecture.Common.Server
  - Module: TimeWarp.Architecture
  - Interfaces: TimeWarp.Architecture.Common.Server
  - Base classes: TimeWarp.Architecture.Common.Server
  - CORS: TimeWarp.Architecture.Common.Server.CorsPolicy
  - Extensions: TimeWarp.Architecture.Common.Server.Extensions

### Phase 5: Update Referencing Projects
Update these 4 projects' ProjectReference paths to Common.Server:
- [ ] Api.Server.csproj — currently references `..\..\..\Common\Common.Server\Common.Server.csproj`
- [ ] Grpc.Server.csproj — currently references `..\..\..\Common\Common.Server\Common.Server.csproj`
- [ ] Web.Server.csproj — currently references `..\..\..\Common\Common.Server\Common.Server.csproj`
- [ ] Yarp.csproj — currently references `..\..\Common\Common.Server\Common.Server.csproj`

### Phase 6: Update Solution Files
- [ ] Update TimeWarp.Architecture/TimeWarp.Architecture.slnx — redirect Common.Server project path
- [ ] Update timewarp-architecture.slnx — add foundation-server project under /source/common/

### Phase 7: Cleanup and Verify
- [ ] Remove old Common.Server/ directory
- [ ] Build verify foundation-server individually: `dotnet build source/common/foundation-server/foundation-server.csproj`
- [ ] Build verify timewarp-architecture.slnx (now 13 projects)

## Notes

### ProjectReference Path from New Location
From source/common/foundation-server/:
- foundation-infrastructure: `..\foundation-infrastructure\foundation-infrastructure.csproj` (sibling)

### Referencing Project Path Changes
For projects in TimeWarp.Architecture/Source/ContainerApps/Api/Api.Server/:
- Before: `..\..\..\Common\Common.Server\Common.Server.csproj` (3 up to ContainerApps/, down to Common/)
- After: `..\..\..\..\..\..\..\source\common\foundation-server\foundation-server.csproj` (7 up to root, into source/)

For projects in TimeWarp.Architecture/Source/ContainerApps/Yarp/:
- Before: `..\..\Common\Common.Server\Common.Server.csproj` (2 up to Source/, down to Common/)
- After: `..\..\..\..\..\..\source\common\foundation-server\foundation-server.csproj` (6 up to root, into source/)

### Namespace Decisions
- Namespace stays `TimeWarp.Architecture.Common.Server` (most types)
- Namespace stays `TimeWarp.Architecture` (CommonServerModule)
- Namespace stays `TimeWarp.Architecture.Common.Server.CorsPolicy` (CORS types)
- Namespace stays `TimeWarp.Architecture.Common.Server.Extensions` (extension types)
- Future rename to `TimeWarp.Foundation.*` is task 051

### Key Functionality
Common.Server provides:
- **CommonServerModule**: Azure App Configuration integration, Scalar API docs, FluentValidation config
- **BaseEndpoint/BaseFastEndpoint**: Base classes for FastEndpoints
- **CORS policies**: AnyPolicy (development), ExamplePolicy (production-ready example)
- **ServiceUriHelper**: URI resolution for container communication
- **MvcBuilderExtensions**: MVC configuration helpers

## Results

To be filled after completion.

## Session
- Created: ses_2d78597cfffeIe36aerm1ibchw (2026-04-21)