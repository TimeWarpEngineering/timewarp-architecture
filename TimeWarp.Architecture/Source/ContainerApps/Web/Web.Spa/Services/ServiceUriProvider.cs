namespace TimeWarp.Architecture.Services;

public class ServiceUriProvider
{
  private readonly IHttpClientFactory HttpClientFactory;
  private readonly ILogger<ServiceUriProvider> Logger;
  public Dictionary<string, Uri> ServiceUris { get; private set; } = new();
  private bool IsInitialized = false;

  public ServiceUriProvider(
    IHttpClientFactory httpClientFactory,
    ILogger<ServiceUriProvider> logger)
  {
    HttpClientFactory = httpClientFactory;
    Logger = logger;
  }

  public async Task InitializeAsync(CancellationToken cancellationToken)
  {
    if (IsInitialized) return;

    try
    {
      HttpClient httpClient = HttpClientFactory.CreateClient(Constants.WebServiceName);

      Logger.LogInformation("Fetching service discovery information.");
      HttpResponseMessage response = await httpClient.GetAsync("/service-discovery", cancellationToken);
      response.EnsureSuccessStatusCode();

      ServiceUris = await response.Content.ReadFromJsonAsync<Dictionary<string, Uri>>(cancellationToken: cancellationToken)
        ?? throw new InvalidOperationException("Service discovery information is null");

      Logger.LogInformation("Service discovery information fetched successfully.");
      IsInitialized = true;
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "Failed to fetch service discovery information.");
      throw;
    }
  }
}
