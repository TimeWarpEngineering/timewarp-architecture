#region Purpose
// Project-wide using directives so individual files omit repeated imports.
#endregion

global using Aspire.Customization.AppHost;
#if(postgres)
global using Aspire.Hosting.EntityFrameworkCore;
#endif
#if(yarp)
global using Aspire.Hosting.Yarp;
global using Aspire.Hosting.Yarp.Transforms;
#endif
global using Microsoft.Extensions.Diagnostics.HealthChecks;
global using System.Diagnostics;
global using static TimeWarp.Architecture.Aspire.Constants;
