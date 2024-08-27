namespace MyBlazorApp.Client.Services;

public class AuthService
{
  private readonly HttpClient HttpClient;
  private readonly IJSRuntime JsRuntime;

  public AuthService(HttpClient httpClient, IJSRuntime jsRuntime)
  {
    HttpClient = httpClient;
    JsRuntime = jsRuntime;
  }

  public async Task<bool> RegisterUser(string alias)
  {
    HttpResponseMessage response = await HttpClient.PostAsJsonAsync(requestUri: "api/register", new { alias });
    if (response.IsSuccessStatusCode)
    {
      string token = await response.Content.ReadAsStringAsync();
      await JsRuntime.InvokeVoidAsync(identifier: "passwordless.register", token);
      return true;
    }
    return false;
  }

    public async Task<bool> SignIn()
  {
    string response = await HttpClient.GetStringAsync(requestUri: "api/signin-token");
    VerifiedUser result = await JsRuntime.InvokeAsync<VerifiedUser>(identifier: "passwordless.signin", response);
    if (result != null)
    {
      // Handle successful sign-in
      return true;
    }
    return false;
  }
}
