#region Purpose
// Marks a list or search query contract as a resource type the Ask composer can reference with @.
#endregion

#region Design
// Task 345. A resource type is a list query, not a page fact. The attribute goes on the outer
// [ApiEndpoint] contract class beside [EndpointAuthorize]. The query runs as the signed-in
// user, so that policy decides which types a person may list and which items come back; a
// token grants nothing, and the server re-authorizes every action that later uses the id.
// Shape (TWA0033, ReferenceableResourceAnalyzer): the nested Query has a settable string Search;
// the nested Response has an enumerable ItemsProperty (default "Items"); each item has IdProperty,
// whose type is a [TypedId] struct or Guid, and LabelProperty, a string. Prefix is lowercase
// letters, digits, and '-', starts with a letter, and is unique (TWA0034). The SPA's
// ResourceReferenceRegistryGenerator emits the @ type list from every marked contract, so the
// registry is never hand-maintained. Property names are strings so a contract can use nameof on
// its nested item type.
#endregion

namespace TimeWarp.Architecture.Attributes;

/// <summary>
/// Makes a list query a resource type for <c>@prefix:id</c> references in Ask. Put it on the
/// <c>[ApiEndpoint]</c> contract class. The nested Query must have a settable <c>string? Search</c>,
/// and the Response must list items carrying <see cref="IdProperty"/> and <see cref="LabelProperty"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ReferenceableResourceAttribute : Attribute
{
  /// <param name="prefix">Token prefix, e.g. <c>feedback</c> for <c>@feedback:{id}</c>.</param>
  /// <param name="displayName">Plural name the type list shows, e.g. "Feedback items".</param>
  public ReferenceableResourceAttribute(string prefix, string displayName)
  {
    Prefix = prefix;
    DisplayName = displayName;
  }

  /// <summary>Token prefix: lowercase letters, digits and '-', starting with a letter. Unique.</summary>
  public string Prefix { get; }

  /// <summary>Plural name shown in the type list.</summary>
  public string DisplayName { get; }

  /// <summary>Item property holding the id: a [TypedId] struct or Guid.</summary>
  public string IdProperty { get; set; } = "";

  /// <summary>Item property holding the display label (string). A blank label falls back to the id.</summary>
  public string LabelProperty { get; set; } = "";

  /// <summary>Response property listing the items. Defaults to <c>Items</c>.</summary>
  public string ItemsProperty { get; set; } = "Items";
}
