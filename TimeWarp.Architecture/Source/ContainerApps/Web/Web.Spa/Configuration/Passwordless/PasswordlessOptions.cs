namespace TimeWarp.Architecture.Configuration.Passwordless;

public class PasswordlessOptions
{
  public string ApiUrl { get; set; } = null!;
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
