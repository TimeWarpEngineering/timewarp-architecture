#region Purpose
// Options binding for the Bitwarden Passwordless.dev passkey integration.
#endregion

#region Design
// Validation rules are left minimal on purpose so the template builds and runs without a
// provisioned Passwordless account; the validator exists to keep the options-validation
// pipeline wired for when real rules are enabled.
// Register.Discoverable controls whether created passkeys are discoverable (sign-in without
// first entering an alias).
#endregion

namespace TimeWarp.Architecture.Configuration.Passwordless;

public class PasswordlessOptions
{
  public Uri ApiUrl { get; set; } = null!;
  public string ApiKey { get; set; } = null!;

  public RegisterOptions Register { get; set; } = null!;

  public class RegisterOptions
  {
    public bool Discoverable { get; set; }
  }
}

internal sealed class PasswordlessOptionsValidator : AbstractValidator<PasswordlessOptions>
{
  public PasswordlessOptionsValidator()
  {
    // RuleFor(x => x.ApiKey).NotEmpty();
  }
}
