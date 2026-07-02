#region Purpose
// End-to-end tests for POST api/Roles: real host, real mediator pipeline, real backend validation.
#endregion

namespace CreateRoleEndpoint_;

using static TimeWarp.Architecture.Features.Admin.Roles.CreateRole;

public class Returns_
{
  private readonly Command Command;
  private readonly WebTestServerApplication WebTestServerApplication;

  public Returns_
  (
    WebTestServerApplication webTestServerApplication
  )
  {
    Command = new Command
    {
      UserId = Guid.NewGuid(),
      Name = "Auditor",
      Description = "Read-only access to financial modules."
    };
    WebTestServerApplication = webTestServerApplication;
  }

  public async Task Ok_With_RoleId_Given_Valid_Command()
  {
    OneOf<Response, SharedProblemDetails> result = await WebTestServerApplication.Send(Command);

    result.Switch
    (
      response => response.RoleId.ShouldNotBe(Guid.Empty),
      problemDetails => problemDetails.ShouldBeNull("CreateRole returned SharedProblemDetails for a valid command.")
    );
  }

  public async Task ValidationError_Given_Empty_Name()
  {
    // Backend validation: FluentValidationBehavior runs the contract's Validator
    // (shared RoleDetailsValidator) server-side — the same rules the Blazor form enforced.
    Command.Name = "";

    await WebTestServerApplication.ConfirmEndpointValidationError<Response>(Command, nameof(Command.Name));
  }

  public async Task ValidationError_Given_Empty_UserId()
  {
    // AuthApiRequestValidator composes into the same server-side validation pass.
    Command.UserId = Guid.Empty;

    await WebTestServerApplication.ConfirmEndpointValidationError<Response>(Command, nameof(Command.UserId));
  }
}
