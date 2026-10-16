global using Aspire.Hosting;
global using Aspire.Hosting.ApplicationModel;
#if(web)
// Web-only ingress-smoke-tests.cs is the sole consumer of these (task 373).
global using Microsoft.Extensions.DependencyInjection;
global using System.Collections.Concurrent;
global using System.Net;
#endif
global using System.Text.RegularExpressions;
global using TimeWarp.Architecture.Testing;
