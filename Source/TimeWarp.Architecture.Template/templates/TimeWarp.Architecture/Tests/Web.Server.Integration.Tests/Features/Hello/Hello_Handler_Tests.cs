namespace Hello_Handler;

using static TimeWarp.Architecture.Features.Hello.Contracts.Hello;

public class Handle_Returns
{
  private readonly Query Query;
  private readonly WebTestServerApplication WebTestServerApplication;

  public Handle_Returns
  (
     WebTestServerApplication aWebTestServerApplication
  )
  {
    Query = new Query { Name = "SomeEvent" };
    WebTestServerApplication = aWebTestServerApplication;
  }

  public async Task Ok_Given_Valid_Request()
  {
    OneOf<Response, SharedProblemDetails> result = await WebTestServerApplication.Send(Query);

    ValidateResult(result);
  }

  private void ValidateResult(OneOf<Response, SharedProblemDetails> result)
  {
    result.Switch(
        response =>
        {
          response.Should().NotBeNull();
        },
        problemDetails =>
        {
          // This should not happen in a successful case
          Execute.Assertion.FailWith("The SignIn handler returned SharedProblemDetails instead of a successful response.");
        }
    );
  }
}
