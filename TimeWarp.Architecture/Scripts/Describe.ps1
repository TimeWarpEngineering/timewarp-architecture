# https://jasperfx.github.io/oakton/documentation/hostbuilder/describe/

# Uses the Oakton library to dump valuable information as below.

# ── About EnvironmentCheckDemonstrator ──────────────────────────────────────────
#           Entry Assembly: EnvironmentCheckDemonstrator
#                  Version: 1.0.0.0
#         Application Name: EnvironmentCheckDemonstrator
#              Environment: Production
#        Content Root Path: /Users/jeremydmiller/code/oakton/src/EnvironmentCheckDemonstrator
# AppContext.BaseDirectory: /Users/jeremydmiller/code/oakton/src/EnvironmentCheckDemonstrator/bin/Debug/net5.0/


# ── Referenced Assemblies ────────────────────────────────────────────────────────
# ┌───────────────────────────────────────────────────────┬─────────┐
# │ Assembly Name                                         │ Version │
# ├───────────────────────────────────────────────────────┼─────────┤
# │ System.Runtime                                        │ 5.0.0.0 │
# │ Oakton                                                │ 3.0.0.0 │
# │ Microsoft.Extensions.DependencyInjection.Abstractions │ 5.0.0.0 │
# │ System.ComponentModel                                 │ 5.0.0.0 │
# │ System.Console                                        │ 5.0.0.0 │
# │ Microsoft.Extensions.Hosting                          │ 5.0.0.0 │
# │ Microsoft.Extensions.Hosting.Abstractions             │ 5.0.0.0 │
# │ Baseline                                              │ 2.1.1.0 │
# └───────────────────────────────────────────────────────┴─────────┘

dotnet run --project .\Source\ContainerApps\Web\Web.Server\Web.Server.csproj -- describe
