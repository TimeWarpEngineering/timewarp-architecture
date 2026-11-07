#region Purpose
// Every web-spa TimeWarp.State state serializes through StateJsonContext, the type info State
// DevTools snapshots use (no Aspire host).
#endregion

#region Design
// Task 484: TWS0013 makes the build list each state on StateJsonContext, but a listed state can still
// throw at serialize time (an unsupported property type, a converter the context cannot reach). This
// fact walks every concrete IState in the web-spa assembly, takes it from a real store (so Initialize
// has run), and serializes it with the context alone, no reflection fallback. A state the context does
// not list fails on GetTypeInfo. C-create AnalyticsSpaTestApplication: the store needs no live BFF.
#endregion

namespace StateJsonContext_;

using System.Text.Json.Serialization.Metadata;
using TimeWarp.Architecture.Web.Spa.Integration.Tests.Features.Analytics;
using TimeWarp.Architecture.Web.Spa;

[TestTag("Integration")]
public class Serialize_Should
{
  private static AnalyticsSpaTestApplication? Spa;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Serialize_Should>();

  public static Task SetupOnce()
  {
    Spa = new AnalyticsSpaTestApplication();
    return Task.CompletedTask;
  }

  public static Task CleanUpOnce()
  {
    Spa?.Dispose();
    Spa = null;
    return Task.CompletedTask;
  }

  public static Task Serialize_Every_State_Without_Reflection()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    JsonSerializerOptions options =
      new(JsonSerializerDefaults.Web) { TypeInfoResolver = StateJsonContext.Default };

    Type[] stateTypes =
      [..
        typeof(IAssemblyMarker).Assembly.GetTypes()
          .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IState).IsAssignableFrom(type))
          .OrderBy(type => type.FullName, StringComparer.Ordinal)
      ];
    stateTypes.ShouldNotBeEmpty();

    foreach (Type stateType in stateTypes)
    {
      JsonTypeInfo typeInfo = options.GetTypeInfo(stateType);
      IState state = scope.Store.GetState(stateType);

      string json = JsonSerializer.Serialize(state, typeInfo);

      json.ShouldStartWith("{", customMessage: stateType.FullName);
    }

    return Task.CompletedTask;
  }
}
