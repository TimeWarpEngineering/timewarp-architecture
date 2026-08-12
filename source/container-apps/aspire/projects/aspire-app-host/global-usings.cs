#region Purpose
// Project-wide using directives so individual files omit repeated imports.
#endregion

// WithScalar lives here and is only applied to the api project resource.
#if(api)
global using Aspire.Customization.AppHost;
#endif
#if(postgres)
#endif
#if(yarp)
global using Aspire.Hosting.Yarp;
global using Aspire.Hosting.Yarp.Transforms;
#endif
global using Microsoft.Extensions.Diagnostics.HealthChecks;
global using System.Diagnostics;
global using static TimeWarp.Architecture.Aspire.Constants;
