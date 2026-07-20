#region Purpose
// Global usings for the agent-identity demo CLI.
#endregion

global using System;
global using System.Buffers.Text;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Net.Http;
global using System.Net.Http.Headers;
global using System.Security.Cryptography;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Threading;
global using System.Threading.Tasks;

global using Microsoft.Extensions.DependencyInjection;
global using TimeWarp.Identity;
global using TimeWarp.Nuru;
global using TimeWarp.Terminal;
global using static TimeWarp.Nuru.Unit;

global using AgentIdentityCli.Services;
