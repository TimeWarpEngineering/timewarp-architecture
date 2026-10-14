#region Purpose
// Marks a logged value as DataTaxonomy.SensitiveData so redaction erases it before any log provider sees it.
#endregion

namespace TimeWarp.Architecture.Compliance;

using Microsoft.Extensions.Compliance.Classification;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field)]
public sealed class SensitiveDataAttribute : DataClassificationAttribute
{
  public SensitiveDataAttribute() : base(DataTaxonomy.SensitiveData) { }
}
