global using FakeItEasy;
global using FluentAssertions;
global using MediatR;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Hosting;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection.Extensions;
global using Microsoft.Extensions.Options;
global using Microsoft.JSInterop;
global using System.Diagnostics.CodeAnalysis;
global using System.Net;
global using System.Net.Http;
global using System.Text.Json;

// Solution usings
global using TimeWarp.Architecture.Features;
global using TimeWarp.Architecture.Types;
global using TimeWarp.Fixie;

#if(web)
global using TimeWarp.Architecture.Features.ClientLoaders;
global using TimeWarp.Architecture.Web.Spa;
#endif
