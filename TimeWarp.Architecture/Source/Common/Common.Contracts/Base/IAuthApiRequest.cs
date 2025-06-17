namespace TimeWarp.Architecture.Features;

public interface IAuthApiRequest : IApiRequest
{
  /// <summary>
  /// The User Id of the current user.
  /// </summary>
  /// <remarks>This should match the NameIdentifier claim.
  /// The Server must always validate the token before trusting any claim.
  /// The UserId should equal the NameIdentifier is a secondary check.</remarks>
  /// <remarks>This facilitates The MockAPI to give better responses to exercise the UX.</remarks>
  public Guid UserId { get; set; }
}

public sealed class AuthApiRequestValidator : AbstractValidator<IAuthApiRequest>
{
  public AuthApiRequestValidator()
  {
    RuleFor(r => r.UserId).NotEmpty();
  }
}
