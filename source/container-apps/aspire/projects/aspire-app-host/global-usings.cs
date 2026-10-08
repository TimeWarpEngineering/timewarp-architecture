#region Purpose
// Project-wide using directives so individual files omit repeated imports.
#endregion

// WithScalar lives here and is only applied to the api project resource.
#if(api)
global using Aspire.Customization.AppHost;
#endif
#if(web)
#if(postgres)
// IsDevelopment gates the Postgres WithRepl dashboard command; Postgres is declared only inside the
// web block (program.cs), so the using is nested the same way to stay IDE0005-clean in every combination.
global using Microsoft.Extensions.Hosting;
#endif
#endif
#if(yarp)
global using Aspire.Hosting.Yarp;
global using Aspire.Hosting.Yarp.Transforms;
#endif
// Task 070-004: KubernetesEnvironmentResource (the Helm publish target) is referenced in every flag combination.
global using Aspire.Hosting.Kubernetes;
// Task 070-007: AzureContainerAppEnvironmentResource (the aca publish target), likewise in every combination.
global using Aspire.Hosting.Azure.AppContainers;
global using Microsoft.Extensions.Diagnostics.HealthChecks;
global using System.Diagnostics;
global using static TimeWarp.Architecture.Aspire.Constants;
