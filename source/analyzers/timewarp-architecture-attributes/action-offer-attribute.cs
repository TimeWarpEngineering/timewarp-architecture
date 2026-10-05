#region Purpose
// Marks a contracts record as the typed arguments of one server offer and names the client catalog action it offers (TWA0029/TWA0030).
#endregion

#region Design
// Hypermedia approach B (tasks 275, 279, 280): the server offers a client [CatalogAction] by name with
// arguments keyed by that action's constructor parameters. Server code cannot reference SPA action
// types, so the agreement lives in the shared contracts: one record per offerable action, whose
// properties ARE the arguments the server binds (serialized camelCase, the catalog parameter names),
// tagged with the catalog name it offers. The client action sets [CatalogAction(Name = …)] to the
// same constant. The SPA compilation sees both sides, so TWA0029 (no action with that Name) and
// TWA0030 (record properties vs constructor parameters) check the agreement at build time.
// UserInput lists the required action parameters the offer deliberately leaves for the user to supply
// (Rename's nickname); TWA0030 verifies each one is a required parameter no property binds, and that
// every other required parameter has a property.
// Matched by simple name, like every convention-analyzer attribute.
#endregion

namespace TimeWarp.Architecture.Attributes;

/// <summary>
/// Declares that this record carries the arguments of a server offer for the client catalog action
/// <see cref="CatalogName"/>. Its public properties bind the action's constructor parameters by
/// camelCase name; TWA0029 / TWA0030 check the pairing in the client compilation.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class ActionOfferAttribute : Attribute
{
  /// <summary>The offered action's <c>[CatalogAction(Name = …)]</c>.</summary>
  public string CatalogName { get; }

  /// <summary>Required action parameters the offer leaves for the user to supply (camelCase parameter names).</summary>
  public string[] UserInput { get; set; } = [];

  /// <summary>Marks the record as the arguments of an offer for <paramref name="catalogName"/>.</summary>
  public ActionOfferAttribute(string catalogName)
  {
    CatalogName = catalogName;
  }
}
