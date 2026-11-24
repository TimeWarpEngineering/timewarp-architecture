#region Purpose
// Lets a FormField ask whether an agent draft wrote the property it edits.
#endregion

#region Design
// FormField is a Tier-1 component and does not reference the agent feature. FormPrefillScope
// cascades this interface, so the badge rule lives in one place and the field only names its
// property.
#endregion

namespace TimeWarp.Architecture.Components;

/// <summary>Answers whether an agent draft filled a working-copy property that the person has not changed since.</summary>
public interface IAgentFilledFields
{
  bool IsAgentFilled(string propertyName);
}
