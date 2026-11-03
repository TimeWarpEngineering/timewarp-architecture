#region Purpose
// MSBuild options for FastEndpointSourceGenerator: EnableApiEndpointGeneration and the
// ApiEndpointContractAssemblies allow-list, as an equatable pipeline value.
#endregion

namespace TimeWarp.Architecture.Analyzers;

/// <summary>Equatable generation options so unrelated host edits do not rebuild every endpoint.</summary>
internal sealed class FastEndpointGenerationOptions : IEquatable<FastEndpointGenerationOptions>
{
  public bool Enabled { get; }
  public ImmutableArray<string> ContractAssemblies { get; }

  private FastEndpointGenerationOptions(bool enabled, ImmutableArray<string> contractAssemblies)
  {
    Enabled = enabled;
    ContractAssemblies = contractAssemblies;
  }

  public static FastEndpointGenerationOptions Read(AnalyzerConfigOptions globalOptions)
  {
    bool enabled = false;
    if (globalOptions.TryGetValue("build_property.EnableApiEndpointGeneration", out string? enableValue) &&
        bool.TryParse(enableValue, out bool parsed))
    {
      enabled = parsed;
    }

    ImmutableArray<string> contractAssemblies = ImmutableArray<string>.Empty;
    if (globalOptions.TryGetValue("build_property.ApiEndpointContractAssemblies", out string? assembliesValue) &&
        !string.IsNullOrWhiteSpace(assembliesValue))
    {
      contractAssemblies = assembliesValue
        .Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
        .ToImmutableArray();
    }

    return new FastEndpointGenerationOptions(enabled, contractAssemblies);
  }

  public bool Equals(FastEndpointGenerationOptions? other)
    => other is not null
       && Enabled == other.Enabled
       && ContractAssemblies.SequenceEqual(other.ContractAssemblies, StringComparer.OrdinalIgnoreCase);

  public override bool Equals(object? obj) => Equals(obj as FastEndpointGenerationOptions);

  public override int GetHashCode()
  {
    HashCode hashCode = new();
    hashCode.Add(Enabled);
    foreach (string name in ContractAssemblies)
    {
      hashCode.Add(name, StringComparer.OrdinalIgnoreCase);
    }

    return hashCode.ToHashCode();
  }
}
