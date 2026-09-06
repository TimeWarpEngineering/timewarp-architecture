#region Purpose
// Project-wide using directives so individual files omit repeated imports.
#endregion

#pragma warning disable IDE0005 // Global usings are consumed across .cs and .razor; per-line unused is a false positive after razor/@code splits.

global using Ardalis.GuardClauses;
global using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;
global using TimeWarp.State;
global using Blazored.LocalStorage;
global using Blazored.SessionStorage;
global using FluentValidation;
global using FluentValidation.Results;
global using Grpc.Net.Client.Web;
global using Grpc.Net.Client;
global using TimeWarp.Mediator;
global using TimeWarp.Mediator.Pipeline;
global using Microsoft.AspNetCore.Authorization;
global using Microsoft.AspNetCore.Components.Forms;
global using Microsoft.AspNetCore.Components.Web;
global using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
global using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
global using Microsoft.AspNetCore.Components;
global using Microsoft.AspNetCore.Components.Authorization;
global using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;
global using Microsoft.AspNetCore.SignalR.Client;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
global using Microsoft.FluentUI.AspNetCore.Components;
global using Microsoft.FluentUI.AspNetCore.Components.Utilities;
global using Microsoft.JSInterop;
global using OneOf;
global using TimeWarp.Architecture.Common.Interfaces;
global using TimeWarp.Features.ReduxDevTools;
global using TimeWarp.Features.Routing;
#if(grpc)
global using ProtoBuf.Grpc.Client;
#endif

global using System;
global using System.Diagnostics.CodeAnalysis;
global using System.Globalization;
// HttpStatusCode for mock-api-service (api-flag surface); without api the mock is excluded → IDE0005.
#if(api)
global using System.Net;
#endif
global using System.Net.Http;
global using System.Net.Http.Json;
global using System.Reflection;
global using System.Security.Claims;
global using System.Text.Json;
global using System.Text.RegularExpressions;
global using System.Threading;
global using System.Threading.Tasks;

// Solution usings
global using TimeWarp.Architecture.Components;

// Base component hierarchy now comes from the TimeWarp.Components package (was defined
// inline under TimeWarp.Architecture.Components). Aliased rather than whole-namespace
// imported to avoid clashing with the FluentUI CssBuilder
// (Microsoft.FluentUI.AspNetCore.Components.Utilities) imported above. Migrating the
// CssBuilder usage to TimeWarp.Components is a follow-up (needs API parity).
global using ParentComponent = TimeWarp.Components.ParentComponent;
global using IAttributeComponent = TimeWarp.Components.IAttributeComponent;
global using TimeWarp.Architecture.Configuration;
global using TimeWarp.Architecture.Features;
global using TimeWarp.Architecture.Features.Account;
global using TimeWarp.Architecture.Features.Applications;
global using TimeWarp.Architecture.Features.Authentication;
global using TimeWarp.Architecture.Features.Authorization;
global using TimeWarp.Architecture.Features.Identity;
global using TimeWarp.Architecture.Features.Chat;
global using TimeWarp.Architecture.Features.EventStreams;
global using TimeWarp.Architecture.Features.Profiles;
global using TimeWarp.Architecture.Features.AgentLinks;
global using TimeWarp.Architecture.Hubs;
global using TimeWarp.Architecture.Pipeline.NotificationPostProcessor;
global using TimeWarp.Architecture.Services;
global using TimeWarp.Features.ActionTracking;
global using TimeWarp.Features.StateTransactions;
global using TimeWarp.State.Extensions;
global using TimeWarp.State.Plus.State;
global using static TimeWarp.Architecture.AuthorizationConstants;
global using TimeWarp.Architecture.Features.Counters;
#if(grpc)
global using TimeWarp.Architecture.Features.Superheros;
#endif
#if(api)
// global using TimeWarp.Architecture.Features.WeatherForecast.Pages;
global using TimeWarp.Architecture.Features.WeatherForecasts;
#endif

global using TimeWarp.Foundation;
global using TimeWarp.Foundation.Features;
global using TimeWarp.Foundation.Types;
global using IBaseRequest = TimeWarp.Foundation.Features.IBaseRequest;
global using TimeWarp.Foundation.Configuration;
