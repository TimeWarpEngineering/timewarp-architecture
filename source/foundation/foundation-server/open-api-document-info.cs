#region Purpose
// Host-supplied OpenAPI document facts: info.contact.url, info.license, and the root tag descriptions.
#endregion

#region Design
// Contact and license come from repository metadata, not hand-written strings. The host project
// imports msbuild/open-api-document-metadata.props, which bakes PackageProjectUrl (or
// RepositoryUrl) and PackageLicenseExpression into the assembly as AssemblyMetadata. FromAssembly
// reads those two keys and throws when either is missing, so a host that asks for the metadata
// without importing the props fails at startup instead of publishing a document without them.
// The license is an SPDX expression; OpenAPI 3.1 and later carry it as license.identifier, with
// the same text as the required name.
// Tag descriptions belong to the host: foundation-server does not know the product slices. When
// TagDescriptions is set, OpenApiRootTags fails the document on an operation tag without one.
// A host that passes no OpenApiDocumentInfo still gets root tags, without descriptions.
#endregion

namespace TimeWarp.Foundation;

using Microsoft.OpenApi;

/// <summary>
/// Contact, license, and tag descriptions for the OpenAPI document.
/// </summary>
public sealed class OpenApiDocumentInfo
{
  /// <summary>AssemblyMetadata key that holds the contact URL (PackageProjectUrl, else RepositoryUrl).</summary>
  public const string ContactUrlMetadataKey = "OpenApiContactUrl";

  /// <summary>AssemblyMetadata key that holds the SPDX license expression (PackageLicenseExpression).</summary>
  public const string LicenseMetadataKey = "OpenApiLicense";

  private const string MetadataProps = "msbuild/open-api-document-metadata.props";

  /// <summary>Absolute http(s) URL for info.contact.url.</summary>
  public Uri ContactUrl { get; }

  /// <summary>SPDX license expression for info.license.</summary>
  public string License { get; }

  /// <summary>
  /// Description per operation tag. Null leaves root tags without descriptions and skips the description check.
  /// </summary>
  public IReadOnlyDictionary<string, string>? TagDescriptions { get; }

  /// <summary>Creates the document facts.</summary>
  public OpenApiDocumentInfo(Uri contactUrl, string license, IReadOnlyDictionary<string, string>? tagDescriptions)
  {
    ArgumentNullException.ThrowIfNull(contactUrl);
    ArgumentException.ThrowIfNullOrWhiteSpace(license);
    if (!IsHttpUrl(contactUrl))
    {
      throw new ArgumentException($"The contact URL must be an absolute http(s) URL: {contactUrl}", nameof(contactUrl));
    }

    ContactUrl = contactUrl;
    License = license;
    TagDescriptions = tagDescriptions;
  }

  /// <summary>
  /// Reads contact URL and license from <paramref name="assembly"/>'s AssemblyMetadata, written by
  /// <c>msbuild/open-api-document-metadata.props</c>.
  /// </summary>
  public static OpenApiDocumentInfo FromAssembly(Assembly assembly, IReadOnlyDictionary<string, string>? tagDescriptions)
  {
    ArgumentNullException.ThrowIfNull(assembly);
    string contactUrl = Metadata(assembly, ContactUrlMetadataKey);
    if (!Uri.TryCreate(contactUrl, UriKind.Absolute, out Uri? contactUri))
    {
      throw new InvalidOperationException
      (
        $"OpenApiDocumentInfo: AssemblyMetadata {ContactUrlMetadataKey} on {assembly.GetName().Name} is not an absolute URL: "
        + $"{contactUrl}. Set PackageProjectUrl or RepositoryUrl in Directory.Build.props."
      );
    }

    return new OpenApiDocumentInfo(contactUri, Metadata(assembly, LicenseMetadataKey), tagDescriptions);
  }

  internal void Register(OpenApiOptions openApiOptions)
  {
    ArgumentNullException.ThrowIfNull(openApiOptions);
    openApiOptions.AddDocumentTransformer(ApplyAsync);
  }

  private Task ApplyAsync
  (
    OpenApiDocument document,
    OpenApiDocumentTransformerContext context,
    CancellationToken cancellationToken
  )
  {
    _ = context;
    _ = cancellationToken;
    document.Info ??= new OpenApiInfo();
    document.Info.Contact = new OpenApiContact { Url = ContactUrl };
    document.Info.License = new OpenApiLicense { Name = License, Identifier = License };
    return Task.CompletedTask;
  }

  private static string Metadata(Assembly assembly, string key)
  {
    string? value = assembly
      .GetCustomAttributes<AssemblyMetadataAttribute>()
      .FirstOrDefault(attribute => string.Equals(attribute.Key, key, StringComparison.Ordinal))?
      .Value;
    if (string.IsNullOrWhiteSpace(value))
    {
      throw new InvalidOperationException
      (
        $"OpenApiDocumentInfo: {assembly.GetName().Name} has no AssemblyMetadata {key}. "
        + $"Import $(RepositoryRoot){MetadataProps} in the host project."
      );
    }

    return value;
  }

  private static bool IsHttpUrl(Uri uri) =>
    uri.IsAbsoluteUri
    && (string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
      || string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase));
}
