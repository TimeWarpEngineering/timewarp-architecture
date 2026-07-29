namespace HelloEndpoint_;

using static TimeWarp.Architecture.Features.Hellos.Hello;

public class Returns_
(
  WebTestServerApplication webTestServerApplication
)
{
  public async Task Ok_Given_Valid_Request()
  {
    Query query = new() { Name = "Bob" };

    OneOf<Response, FileResponse, SharedProblemDetails> response =
      await webTestServerApplication.GetResponse<Response>(query, new CancellationToken());

    response.Switch
    (
      ValidateResponse,
      _ => throw new Exception("File response returned"),
      problemDetails => throw new Exception($"Problem details returned: Status={problemDetails.Status}, Title={problemDetails.Title}, Detail={problemDetails.Detail}, Type={problemDetails.Type}, Extensions={System.Text.Json.JsonSerializer.Serialize(problemDetails.Extensions)}")
    );
  }

  public async Task ValidationError()
  {
    Query query = new() { Name = "" };

    await webTestServerApplication.ConfirmEndpointValidationError<Response>
    (
      query,
      nameof(query.Name)
    );
  }

  private static void ValidateResponse(Response response)
  {
    response.ShouldNotBeNull();
    response.Message.ShouldBe("Hello, Bob!");
  }
}
