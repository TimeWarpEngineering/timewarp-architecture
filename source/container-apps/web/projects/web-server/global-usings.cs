#region Purpose
// Project-wide using directives so individual files omit repeated imports.
#endregion

global using TimeWarp.Mediator;
global using TimeWarp.Mediator.Pipeline;
global using FastEndpoints;
global using FluentValidation;
global using Microsoft.AspNetCore.Authentication;
global using Microsoft.AspNetCore.Authentication.Cookies;
global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Hosting;
global using Microsoft.AspNetCore.Hosting.Server.Features;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.ResponseCompression;
global using Microsoft.AspNetCore.Routing;
global using Microsoft.AspNetCore.SignalR;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.Diagnostics.HealthChecks;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Options;
global using Microsoft.Identity.Web;
global using Microsoft.JSInterop;
#if(postgres)
global using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
#endif
global using Oakton;
global using Oakton.Environment;
global using OneOf;
global using OneOf.Types;
global using Serilog.Core;
global using Serilog.Debugging;
global using System.IO;
global using System.Net;
global using System.Net.Http;
global using System.Net.Mime;
global using System.Reflection;
global using System.Threading;
global using System.Threading.Tasks;

// Solution usings
global using static TimeWarp.Architecture.Aspire.Constants;
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
global using TimeWarp.Architecture.Features;
global using TimeWarp.Architecture.Features.MeteredCapability.Application;
global using TimeWarp.Architecture.Features.Tip.Application;
global using TimeWarp.X402;
global using TimeWarp.Architecture.Hubs;
global using TimeWarp.Identity;
#if(postgres)
global using TimeWarp.Architecture.Persistence;
#endif
global using TimeWarp.Architecture.Services;
global using TimeWarp.Foundation.Services;
global using TimeWarp.Foundation.Types;
global using TimeWarp.Modules;
#if(postgres)
global using TimeWarp.Architecture.HostedServices;
global using TimeWarp.Architecture.Modules;
#endif
global using TimeWarp.Foundation;
global using TimeWarp.Foundation.Features;
