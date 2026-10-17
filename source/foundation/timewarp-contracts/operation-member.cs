#region Purpose
// One derived member of an operation request: name, type, required, default, allowed values, description.
#endregion

#region Design
// Generators fill this type. This package does not reflect over request classes.
// Name is the property identifier. Required follows the required modifier, otherwise a
// non-nullable type with no initializer; a nullable annotation is optional unless required
// is also present, and required wins. Default is the initializer text, omitted when absent.
// AllowedValues are the enum's named constants. Description is DescriptionAttribute.
// The full rules are in readme.md.
#endregion

namespace TimeWarp.Contracts;

/// <summary>
/// One member of an operation request, derived by a host generator from the property declaration.
/// </summary>
public sealed class OperationMember
{
  /// <summary>Property identifier, as declared on the request.</summary>
  public required string Name { get; init; }

  /// <summary>Display name of the member type, supplied by the generator.</summary>
  public required string Type { get; init; }

  /// <summary>True when the caller must supply the member.</summary>
  public required bool Required { get; init; }

  /// <summary>Initializer text. Omitted when the property has no initializer.</summary>
  public string? DefaultValue { get; init; }

  /// <summary>Named constants when <see cref="Type"/> is an enum. Omitted otherwise.</summary>
  public IReadOnlyList<string>? AllowedValues { get; init; }

  /// <summary><c>DescriptionAttribute</c> text. Omitted when the property has none.</summary>
  public string? Description { get; init; }
}
