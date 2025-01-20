global using AutoMapper;
global using MediatR;
global using FluentValidation;
global using FluentValidation.AspNetCore;
global using JetBrains.Annotations;
global using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Hosting;
global using Microsoft.AspNetCore.Hosting.Server.Features;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.ResponseCompression;
global using Microsoft.AspNetCore.Routing;
global using Microsoft.AspNetCore.SignalR;
global using Microsoft.Azure.Cosmos;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.Diagnostics.HealthChecks;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Options;
global using Microsoft.Identity.Web;
global using Microsoft.JSInterop;
global using Microsoft.OpenApi.Models;
#if(postgres)
global using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
#endif
global using Oakton;
global using Oakton.Environment;
global using OneOf;
global using OneOf.Types;
global using Passwordless;
global using Serilog.Core;
global using Serilog.Debugging;
global using Swashbuckle.AspNetCore.Annotations;
global using System.IO;
global using System.Net;
global using System.Net.Http;
global using System.Net.Mime;
global using System.Reflection;
global using System.Threading;
global using System.Threading.Tasks;

// Solution usings
global using static TimeWarp.Architecture.Aspire.Constants;
global using TimeWarp.Architecture.Components;
global using TimeWarp.Architecture.Configuration;
global using TimeWarp.Architecture.CorsPolicies;
global using TimeWarp.Architecture.Data;
global using TimeWarp.Architecture.Extensions;
global using TimeWarp.Architecture.Features.Chat;
global using TimeWarp.Architecture.Hubs;
global using TimeWarp.Architecture.Persistence;
global using TimeWarp.Architecture.Services;
global using TimeWarp.Architecture.Types;
global using TimeWarp.Architecture.Web.Infrastructure;
#if(cosmosdb)
global using TimeWarp.Architecture.HostedServices;
#endif
