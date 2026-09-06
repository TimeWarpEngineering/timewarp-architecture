#region Purpose
// Shared editable progressive-profile shape and validation for GetProfile / UpdateProfile.
#endregion

#region Design
// Validating against the interface lets UpdateProfile.Command and GetProfile.Response share one
// rule set via SetValidator so the Profile page form matches the PUT body. Avatar is display-only
// (GetProfile) and is not on this interface. Length literals duplicate Profile.MaxDisplayNameLength
// / MaxEmailLength — contracts must not reference the domain assembly. Email is optional
// (progressive; never a register/session gate). Alias stays required so chrome always has a name
// (GetProfile create-if-missing defaults to "Member").
#endregion

namespace TimeWarp.Architecture.Features.Profiles;

public interface IProfileDetails
{
  public string Alias { get; set; }
  public string? Email { get; set; }
  public string Language { get; set; }
  public string Region { get; set; }
  public string Theme { get; set; }
  public bool Notifications { get; set; }
}

public sealed class ProfileDetailsValidator : AbstractValidator<IProfileDetails>
{
  public const int MaxAliasLength = 100;
  public const int MaxEmailLength = 254;

  public ProfileDetailsValidator()
  {
    RuleFor(details => details.Alias).NotEmpty().MaximumLength(MaxAliasLength);
    RuleFor(details => details.Email)
      .MaximumLength(MaxEmailLength)
      .EmailAddress()
      .When(details => !string.IsNullOrWhiteSpace(details.Email));
    RuleFor(details => details.Language).NotEmpty();
    RuleFor(details => details.Region).NotEmpty();
    RuleFor(details => details.Theme).NotEmpty();
  }
}
