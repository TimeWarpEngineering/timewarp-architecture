namespace HelloEndpoint_;

using static TimeWarp.Architecture.Features.Hellos.Hello;

public class Returns_
(
  IWebApiTestService WebTestServerApplication
)
{
  private readonly Query Query = new()
    { Name = "Bob" };

  [UsedImplicitly]
  public async Task Ok_Given_Valid_Request()
  {
    OneOf<Response, FileResponse, SharedProblemDetails> response =
      await WebTestServerApplication.GetResponse<Response>(Query, new CancellationToken());

    response.Switch
    (
      ValidateResponse,
      _ => throw new Exception("File response returned"),
      _ => throw new Exception("Problem details returned")
    );
  }

  [UsedImplicitly]
  public async Task ValidationError()
  {
    Query.Name = "";

    await WebTestServerApplication.ConfirmEndpointValidationError<Response>
    (
      Query,
      nameof(Query.Name)
    );
  }

  private static void ValidateResponse(Response response)
  {
    response.Should().NotBeNull();
    response.Message.Should().Be("Hello, Bob!");
  }
}
