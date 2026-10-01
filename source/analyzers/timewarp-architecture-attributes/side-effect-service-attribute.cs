#region Purpose
// Marks a first-party service whose members are side effects only action handlers may call (TWA0026).
#endregion

#region Design
// TWA0026 lists framework side-effect types (NavigationManager, IJSRuntime, HttpClient, browser
// storage, IApiService) in the analyzer because they cannot carry our attribute. First-party
// ceremony clients and JS-module wrappers are expected to multiply, so they declare themselves with
// this marker instead of an analyzer edit per service. Any call from a component member to a
// member of a marked type (or a type that implements / derives from one) is reported.
// Matched by simple name, like every convention-analyzer attribute.
#endregion

namespace TimeWarp.Architecture.Attributes;

/// <summary>
/// Declares that calling this service is a side effect that belongs in a TimeWarp.State action
/// handler; components that call it directly are reported by TWA0026.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class SideEffectServiceAttribute : Attribute;
