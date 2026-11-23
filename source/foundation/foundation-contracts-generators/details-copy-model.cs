#region Purpose
// Shapes the details-copy generator turns into From and Clone source.
#endregion

#region Design
// One member description feeds both emitters. Collections name the concrete copy (List, HashSet,
// Dictionary, or a rank-1 array). Anything else is a scalar, and a scalar reference that implements
// ICloneable is cloned instead of shared. WriteKind says whether the copy is assigned in an object
// initializer, through the property, through an explicit interface, or through an UnsafeAccessor
// field ref. Helper and accessor names are filled in after every member is known so identical
// collection shapes share one helper.
#endregion

namespace TimeWarp.Foundation.Contracts.Generators;

internal enum DetailsCollectionKind
{
  None,
  List,
  HashSet,
  Dictionary,
  Array
}

internal enum DetailsWriteKind
{
  Initializer,
  Property,
  Interface,
  UnsafeField
}

internal sealed class DetailsMember
{
  public string ReadName { get; set; } = "";
  public string WriteName { get; set; } = "";
  public string TypeDisplay { get; set; } = "";
  public string ElementDisplay { get; set; } = "";
  public string KeyDisplay { get; set; } = "";
  public string ValueDisplay { get; set; } = "";
  public DetailsCollectionKind Collection { get; set; }
  public bool Nullable { get; set; }
  public bool ElementCloneable { get; set; }
  public bool ElementNullable { get; set; }
  public bool KeyCloneable { get; set; }
  public bool KeyNullable { get; set; }
  public bool ValueCloneable { get; set; }
  public bool ValueNullable { get; set; }
  public bool CloneableReference { get; set; }
  public DetailsWriteKind WriteKind { get; set; }
  public string FieldOwnerDisplay { get; set; } = "";
  public string FieldName { get; set; } = "";
  public string InterfaceDisplay { get; set; } = "";
  public bool ReadViaField { get; set; }
  public string HelperName { get; set; } = "";
  public string AccessorName { get; set; } = "";
}

internal sealed class DetailsFromPart
{
  public required string InterfaceDisplay { get; init; }
  public List<DetailsMember> Members { get; } = [];
}

internal sealed class DetailsPlan
{
  public required string Namespace { get; init; }
  public required string ClassName { get; init; }
  public List<string> Containers { get; } = [];
  public bool EmitClone { get; set; }
  public bool AddCloneable { get; set; }
  public List<DetailsFromPart> Froms { get; } = [];
  public List<DetailsMember> CloneMembers { get; } = [];
}

internal readonly record struct DetailsCopyOutput(string HintName, string Source)
{
  public static DetailsCopyOutput None { get; } = new("", "");
}
