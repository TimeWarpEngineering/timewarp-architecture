#region Purpose
// Bindable assistant name shared by UpdateAssistantName.Command and GetSiteSettings.Response.
#endregion

#region Design
// One field, so the AI page copies the stored name with generated From instead of assigning
// AssistantName by hand. Version stays off this interface: it is the concurrency token, and
// UpdateAssistantName.Command copies it in CopyBeyondDetails from GetSiteSettings.Response.
// The rules are AssistantNameRules, the same check SiteSettings.RenameAssistant runs.
// NotEmpty runs first so a blank name reports "Assistant name is required." The plain-text
// rule runs only when the name is non-blank, so a blank name does not also fail the length rule.
#endregion

namespace TimeWarp.Architecture.Features.Settings;

using TimeWarp.Identity;

public interface IAssistantNameDetails
{
  public string AssistantName { get; set; }
}

public sealed class AssistantNameDetailsValidator : AbstractValidator<IAssistantNameDetails>
{
  public AssistantNameDetailsValidator()
  {
    RuleFor(details => details.AssistantName)
      .NotEmpty()
      .WithMessage("Assistant name is required.");
    RuleFor(details => details.AssistantName)
      .Must(AssistantNameRules.IsValid)
      .When(static details => !string.IsNullOrWhiteSpace(details.AssistantName))
      .WithMessage
      (
        $"Assistant name must be 1-{AssistantNameRules.MaxLength} characters of plain text, "
        + "without markup, control, format or line-separator characters."
      );
  }
}
