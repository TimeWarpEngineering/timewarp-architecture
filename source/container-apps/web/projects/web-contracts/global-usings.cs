#region Purpose
// Project-wide using directives so individual files omit repeated imports.
#endregion

global using Ardalis.GuardClauses;
global using FluentValidation;
global using OneOf;
global using OneOf.Types;
global using Passwordless;
global using System.Collections.Generic;
global using System.Collections.Specialized;
global using System.Reflection;
global using System.Runtime.CompilerServices;
global using TimeWarp.Architecture;
// Attributes: dual-mode MSBuild <Using> in web-contracts.csproj (task 115) — package mode keeps
// the platform namespace; source mode uses $(RootNamespace).Attributes after sourceName rewrite.
global using TimeWarp.Foundation;

global using TimeWarp.Architecture.Features;
global using TimeWarp.Foundation.Features;
// Solution usings
global using TimeWarp.Foundation.Types;
global using TimeWarp.Identity;
global using TimeWarp.Mediator;
