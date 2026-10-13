#region Purpose
// The answer to one referenceable list search: items, whether the list was cut, or a problem.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>Result of <see cref="ResourceReferenceType.SearchAsync"/>.</summary>
public sealed record ResourceReferenceSearch(IReadOnlyList<ResourceReferenceItem> Items, bool HasMore, string? Problem)
{
  public static ResourceReferenceSearch Failed(string problem) => new([], HasMore: false, problem);
}
