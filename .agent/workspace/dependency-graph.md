# Dependency Graph - TimeWarp.Architecture

## Leaf Projects (No Internal Dependencies)

These projects have no ProjectReference to other projects in the solution. They should be migrated first.

| Project | Path |
|---------|------|
| Common.Contracts | Source/Common/Common.Contracts/ |
| Common.Domain | Source/Common/Common.Domain/ |
| TimeWarp.Architecture.Attributes | Source/Analyzers/TimeWarp.Architecture.Attributes/ |
| TimeWarp.Architecture.Analyzers | Source/Analyzers/TimeWarp.Architecture.Analyzers/ |
| TimeWarp.Modules | Source/Libraries/TimeWarp.Modules/ |
| TimeWarp.Automation.Contracts | Source/Libraries/TimeWarp.Automation.Contracts/ |
| Api.Domain | Source/ContainerApps/Api/Api.Domain/ |
| Grpc.Domain | Source/ContainerApps/Grpc/Grpc.Domain/ |
| Web.Domain | Source/ContainerApps/Web/Web.Domain/ |
| Aspire.ServiceDefaults | Source/ContainerApps/Aspire/Aspire.ServiceDefaults/ |

## Level 1 (Depends on Leaf)

| Project | Dependencies |
|---------|--------------|
| Common.Application | Common.Contracts, Common.Domain, TimeWarp.Modules |
| TimeWarp.Automation | TimeWarp.Automation.Contracts |
| Grpc.Contracts | Common.Contracts |
| Api.Contracts | Common.Contracts, TimeWarp.Architecture.Attributes |
| Web.Contracts | Common.Contracts |
| TimeWarp.Architecture.SourceGenerator | TimeWarp.Architecture.Attributes |

## Level 2

| Project | Dependencies |
|---------|--------------|
| Common.Infrastructure | Common.Application |
| Grpc.Application | Common.Application, Grpc.Contracts, Grpc.Domain |
| Api.Application | Common.Application, TimeWarp.Modules, Api.Contracts, Api.Domain |
| Web.Application | Common.Application, Web.Contracts, Web.Domain |

## Level 3

| Project | Dependencies |
|---------|--------------|
| Common.Server | Common.Infrastructure |
| Grpc.Infrastructure | Common.Infrastructure, Grpc.Application |
| Api.Infrastructure | Common.Infrastructure, Api.Application |
| Web.Infrastructure | Common.Infrastructure, Web.Application |

## Level 4

| Project | Dependencies |
|---------|--------------|
| Grpc.Server | Common.Server, Aspire.ServiceDefaults, Grpc.Infrastructure |
| Api.Server | Common.Server, Aspire.ServiceDefaults, Api.Infrastructure, Api.Contracts, TimeWarp.Architecture.SourceGenerator, TimeWarp.Architecture.Attributes |
| Web.Server | Common.Server, Aspire.ServiceDefaults, Web.Infrastructure, Web.Spa |
| Yarp | Common.Server, Aspire.ServiceDefaults |

## Level 5 (Top-Level Apps)

| Project | Dependencies |
|---------|--------------|
| Web.Spa | TimeWarp.Architecture.Analyzers, Api.Contracts, Grpc.Contracts, Web.Contracts |
| Aspire.AppHost | Api.Server, Grpc.Server, Web.Server, Yarp |

## Test Projects

| Project | Dependencies |
|---------|--------------|
| Testing.Common | Api.Server, Web.Server, Yarp |
| Common.Infrastructure.Tests | Common.Infrastructure |
| TimeWarp.Automation.Tests | TimeWarp.Automation, TimeWarp.Automation.Contracts |
| TimeWarp.Architecture.Analyzers.Tests | TimeWarp.Architecture.Analyzers |
| TimeWarp.Architecture.SourceGenerator.Tests | TimeWarp.Architecture.SourceGenerator |
| Api.Server.Integration.Tests | Api.Contracts, Api.Server, Aspire.AppHost, Testing.Common |
| Web.Server.Integration.Tests | Web.Contracts, Web.Server, Testing.Common |
| Web.Spa.Integration.Tests | Web.Spa, Web.Server, Aspire.AppHost, Testing.Common |
| Aspire (Test) | Aspire.AppHost |
| EndToEnd.Playwright.Tests | (none) |

## Migration Order (Leaf to Root)

### Phase 1: Leaf Projects
1. Common.Contracts
2. Common.Domain
3. TimeWarp.Architecture.Attributes
4. TimeWarp.Architecture.Analyzers
5. TimeWarp.Modules
6. TimeWarp.Automation.Contracts
7. Api.Domain
8. Grpc.Domain
9. Web.Domain
10. Aspire.ServiceDefaults

### Phase 2: Level 1
11. Common.Application
12. TimeWarp.Automation
13. Grpc.Contracts
14. Api.Contracts
15. Web.Contracts
16. TimeWarp.Architecture.SourceGenerator

### Phase 3: Level 2
17. Grpc.Application
18. Api.Application
19. Web.Application

### Phase 4: Level 3
20. Common.Infrastructure
21. Grpc.Infrastructure
22. Api.Infrastructure
23. Web.Infrastructure

### Phase 5: Level 4
24. Common.Server
25. Grpc.Server
26. Api.Server
27. Web.Server
28. Yarp

### Phase 6: Top-Level Apps
29. Web.Spa
30. Aspire.AppHost

### Phase 7: Test Projects
31. Testing.Common
32. Common.Infrastructure.Tests
33. TimeWarp.Automation.Tests
34. TimeWarp.Architecture.Analyzers.Tests
35. TimeWarp.Architecture.SourceGenerator.Tests
36. Api.Server.Integration.Tests
37. Web.Server.Integration.Tests
38. Web.Spa.Integration.Tests
39. Aspire (Test)
40. EndToEnd.Playwright.Tests

### Phase 8: Utilities
41. GenTester
