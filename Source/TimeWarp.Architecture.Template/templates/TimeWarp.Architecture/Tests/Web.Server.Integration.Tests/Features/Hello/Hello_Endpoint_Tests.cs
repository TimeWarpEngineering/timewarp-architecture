namespace HelloEndpoint_;

using static TimeWarp.Architecture.Features.Hellos.Hello;

public class Returns_
{
  private readonly Query Query;
  private readonly WebTestServerApplication WebTestServerApplication;

  public Returns_
  (
    WebTestServerApplication aWebTestServerApplication
  )
  {
    Query = new Query { Name = "Bob" };
    WebTestServerApplication = aWebTestServerApplication;
  }

  public async Task Ok_Given_Valid_Request()
  {
    Response response =
      await WebTestServerApplication.GetResponse<Response>(Query);

    ValidateResponse(response);
  }

  public async Task ValidationError()
  {
    Query.Name = "";

    await WebTestServerApplication.ConfirmEndpointValidationError<Response>
    (
      Query,
      nameof(Query.Name)
    );
  }

  private void ValidateResponse(Response response)
  {
    response.Should().NotBeNull();
    response.Message.Should().Be("Hello, Bob!");
  }
}
