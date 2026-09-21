#region Purpose
// Assembly marker that hosted-contracts compilations apply so host generators walk only marked refs.
#endregion

#region Design
// Public (unlike TypedId's internal generated copy): contracts already reference this package and
// must not attach TimeWarp.Architecture.Generators (that package also ships TWA0001, which is not
// a contracts-file rule). Hosts match the simple name ApiEndpointsEmbeddedAttribute so a generated
// internal copy used by tests is equivalent.
#endregion

namespace TimeWarp.Architecture.Attributes;

/// <summary>
/// Assembly marker applied by hosted-contracts compilations so host generators walk only marked
/// referenced assemblies instead of the whole reference closure.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
public sealed class ApiEndpointsEmbeddedAttribute : Attribute;
