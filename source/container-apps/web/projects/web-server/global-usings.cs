#region Purpose
// Project-wide using directives so individual files omit repeated imports.
#endregion

global using TimeWarp.Mediator;
global using FastEndpoints;
global using FluentValidation;
global using Microsoft.AspNetCore.Authentication;
global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Hosting;
global using Microsoft.AspNetCore.ResponseCompression;
global using Microsoft.AspNetCore.Routing;
global using Microsoft.AspNetCore.SignalR;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Options;
global using Microsoft.Identity.Web;
global using Oakton;
global using Oakton.Environment;
global using OneOf;
global using OneOf.Types;
global using Serilog.Debugging;
global using System.IO;
global using System.Net.Mime;
global using System.Reflection;
global using System.Threading;
global using System.Threading.Tasks;

// Solution usings
global using TimeWarp.Foundation.Behaviors;
global using TimeWarp.Architecture.Abstractions;
global using TimeWarp.Architecture.Components;
global using TimeWarp.Architecture.Configuration;
global using TimeWarp.Foundation.Configuration;
global using TimeWarp.Foundation.CorsPolicies;
global using TimeWarp.Foundation.Extensions;
global using TimeWarp.Architecture.Features.Chat;
global using TimeWarp.Architecture.Features.Identity;
global using TimeWarp.Architecture.Features.Identity.Application;
global using TimeWarp.Architecture.Features.Identity.Infrastructure;
global using TimeWarp.Architecture.Features.MeteredCapability.Application;
global using TimeWarp.Architecture.Features.Tip.Application;
global using TimeWarp.X402;
global using TimeWarp.Identity;
global using TimeWarp.Architecture.Services;
global using TimeWarp.Foundation.Types;
global using TimeWarp.Foundation;
global using TimeWarp.Foundation.Features;

// Postgres-only: EF registrations, health-check types, Aspire connection-string constants,
// and IModule for PostgresDbModule (other modules import TimeWarp.Modules file-locally).
// Without this flag, template smoke --postgres false leaves these unused (IDE0005).
#if(postgres)
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Diagnostics.HealthChecks;
global using static TimeWarp.Architecture.Aspire.Constants;
global using TimeWarp.Architecture.Persistence;
global using TimeWarp.Modules;
global using TimeWarp.Architecture.Modules;
#endif
