#region Purpose
// Collector for FastEndpointSourceGenerator: finds [ApiEndpoint] contracts in allow-listed marked
// referenced assemblies, checks the allow-list, and reads contract XML documentation files.
#endregion

namespace TimeWarp.Architecture.Analyzers;

using TimeWarp.Architecture.Analyzers.Models;

/// <summary>Builds equatable EndpointEmitModel values and allow-list findings from the compilation.</summary>
internal static class FastEndpointCollector
{
  internal static ImmutableArray<string> FindMissingContractAssemblies(
    Compilation compilation,
    FastEndpointGenerationOptions options)
  {
    if (!options.Enabled)
    {
      return ImmutableArray<string>.Empty;
    }

    if (options.ContractAssemblies.IsDefaultOrEmpty)
    {
      return ImmutableArray.Create(
        "the property is empty or unset; set it to the hosted contracts AssemblyName (web-contracts, api-contracts)");
    }

    var markedNames = HostedRouteDiscovery.GetMarkedReferencedAssemblies(compilation)
      .Select(static assembly => assembly.Name)
      .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

    ImmutableArray<string>.Builder missing = ImmutableArray.CreateBuilder<string>();
    foreach (string configured in options.ContractAssemblies)
    {
      if (!markedNames.Contains(configured))
      {
        missing.Add(
          $"'{configured}' matches no marked referenced assembly (typo, renamed AssemblyName, or missing [assembly: ApiEndpointsEmbedded])");
      }
    }

    return missing.ToImmutable();
  }

  internal static ImmutableArray<EndpointEmitModel> DiscoverCandidates(
    Compilation compilation,
    ImmutableArray<string> contractAssemblies,
    ContractDocumentation documentation)
  {
    INamedTypeSymbol? apiEndpointAttributeSymbol =
      compilation.GetTypeByMetadataName(HostedRouteDiscovery.ApiEndpointAttributeFullName);
    if (apiEndpointAttributeSymbol is null)
    {
      return ImmutableArray<EndpointEmitModel>.Empty;
    }

    var allowed = contractAssemblies.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

    ImmutableArray<EndpointEmitModel>.Builder builder = ImmutableArray.CreateBuilder<EndpointEmitModel>();

    foreach (IAssemblySymbol assembly in HostedRouteDiscovery.GetMarkedReferencedAssemblies(compilation))
    {
      if (!allowed.Contains(assembly.Name))
      {
        continue;
      }

      foreach (INamespaceSymbol ns in HostedRouteDiscovery.GetAllNamespaces(assembly.GlobalNamespace))
      {
        foreach (INamedTypeSymbol type in ns.GetTypeMembers())
        {
          bool hasApiEndpoint = type.GetAttributes().Any(attr =>
            SymbolEqualityComparer.Default.Equals(attr.AttributeClass, apiEndpointAttributeSymbol)
            || attr.AttributeClass?.Name == HostedRouteDiscovery.ApiEndpointAttributeSimpleName);
          if (!hasApiEndpoint)
          {
            continue;
          }

          INamedTypeSymbol? requestClass = type.GetTypeMembers()
            .FirstOrDefault(static m => m.Name is "Query" or "Command");

          // ClientOnly on outer or nested → not hosted (TWA0020 warns separately).
          if (HostedRouteDiscovery.HasClientOnlyOnOperationOrRequest(type, requestClass))
          {
            continue;
          }

          builder.Add(EndpointEmitModel.FromSymbol(type, documentation));
        }
      }
    }

    builder.Sort(static (left, right) =>
    {
      int namespaceOrder = string.CompareOrdinal(left.Namespace, right.Namespace);
      return namespaceOrder != 0
        ? namespaceOrder
        : string.CompareOrdinal(left.ClassName, right.ClassName);
    });

    return builder.ToImmutable();
  }

  internal static IncrementalValueProvider<ContractDocumentation> ContractDocumentationProvider(
    IncrementalGeneratorInitializationContext context)
  {
    return context.AdditionalTextsProvider
      .Where(static text => text.Path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
      .Select(static (text, cancellationToken) => text.GetText(cancellationToken)?.ToString())
      .Collect()
      .Select(static (texts, _) => ContractDocumentation.FromXml(texts));
  }
}
