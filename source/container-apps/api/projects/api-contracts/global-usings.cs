#region Purpose
// Project-wide using directives so individual files omit repeated imports.
#endregion

global using Ardalis.GuardClauses;
global using FluentValidation;
global using OneOf;
global using System.Collections.Specialized;
// Attributes: dual-mode MSBuild <Using> in api-contracts.csproj (task 115) — package mode keeps
// the platform namespace; source mode uses $(RootNamespace).Attributes after sourceName rewrite.
global using TimeWarp.Foundation.Features;
// Solution usings
global using TimeWarp.Foundation.Types;
global using TimeWarp.Mediator;
