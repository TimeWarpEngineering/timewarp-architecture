#region Purpose
// HTTP smoke of a deployed ingress: the two anonymous paths, with a cold-start budget.
#endregion

#region Design
// Task 393. The paths and the retry rule live in DeployPipeline so tests can lock them without
// a network. This file only performs the GETs. A transport failure or an HTTP 5xx is retried
// until SmokeBudget; a 4xx stops. /api/health is not requested: that check opens Postgres and
// would keep Neon compute awake. Response bodies are not printed.
#endregion

namespace DevCli.Services;

using System.Net.Http;

/// <summary>Outcome of one ingress smoke: whether every path returned success, and the lines to print.</summary>
internal sealed record DeploySmokeResult(bool Ok, IReadOnlyList<string> Lines);

/// <summary>GETs <see cref="DeployPipeline.SmokePaths"/> on a deployed ingress.</summary>
internal static class DeploySmoke
{
  /// <summary>
  /// Requests each smoke path on <paramref name="baseUrl"/>. Retries a cold start. Returns false when a path
  /// does not succeed inside the budget.
  /// </summary>
  internal static async Task<DeploySmokeResult> CheckAsync(string baseUrl, CancellationToken cancellationToken)
  {
    using HttpClient client = new() { Timeout = DeployPipeline.SmokeAttemptTimeout };
    DateTime deadline = DateTime.UtcNow + DeployPipeline.SmokeBudget;
    List<string> lines = [];
    foreach (string path in DeployPipeline.SmokePaths)
    {
      string url = DeployPipeline.SmokeUrl(baseUrl, path);
      while (true)
      {
        cancellationToken.ThrowIfCancellationRequested();
        (int? status, string? error) = await GetAsync(client, url, cancellationToken);
        if (status is >= 200 and <= 299)
        {
          lines.Add($"{status} {path}");
          break;
        }

        if (!DeployPipeline.ShouldRetrySmoke(status) || DateTime.UtcNow >= deadline)
        {
          string detail = status is null ? error ?? "no response" : $"HTTP {status}";
          lines.Add($"failed {path} ({detail})");
          return new DeploySmokeResult(false, lines);
        }

        await Task.Delay(DeployPipeline.SmokeDelay, cancellationToken);
      }
    }

    return new DeploySmokeResult(true, lines);
  }

  private static async Task<(int? Status, string? Error)> GetAsync(HttpClient client, string url, CancellationToken cancellationToken)
  {
    try
    {
      using HttpResponseMessage response = await client.GetAsync(new Uri(url), HttpCompletionOption.ResponseHeadersRead, cancellationToken);
      return ((int)response.StatusCode, null);
    }
    catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
    {
      return (null, exception.Message);
    }
  }
}
