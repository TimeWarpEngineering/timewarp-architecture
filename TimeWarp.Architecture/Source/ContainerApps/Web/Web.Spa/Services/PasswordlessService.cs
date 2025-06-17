namespace TimeWarp.Architecture.Services;

public class PasswordlessService
{
  private readonly IJSRuntime JsRuntime;
  private readonly HttpClient HttpClient;
  private readonly PasswordlessOptions PasswordlessOptions;

  public PasswordlessService(IJSRuntime jsRuntime, HttpClient httpClient, IOptions<PasswordlessOptions> PasswordlessOptionsAccessor)
  {
    JsRuntime = jsRuntime;
    HttpClient = httpClient;
    PasswordlessOptions = PasswordlessOptionsAccessor.Value;
    Console.WriteLine($"PasswordlessOptions.ApiUrl: {PasswordlessOptions.ApiUrl}");
    Console.WriteLine($"PasswordlessOptions.ApiKey: {PasswordlessOptions.ApiKey}");
  }

  public async Task<string> RegisterAsync(string alias)
  {
    // Fetch the registration token from the backend.  This could be Bitwarden or our Server custom API.
    string? registerToken = await HttpClient.GetFromJsonAsync<string>($"{PasswordlessOptions.ApiUrl}/create-token?userId={alias}");

    // Register the token with the end-user's device
    return await JsRuntime.InvokeAsync<string>(identifier: "passwordless.register", registerToken);
  }

  public async Task<string> AuthenticateAsync(string username)
  {
    return await JsRuntime.InvokeAsync<string>(identifier: "passwordless.authenticate", username);
  }
}
