#region Purpose
// Maps the Ask configuration probe's outcome (response, problem, or file) to a readiness result.
#endregion

#region Design
// Task 293. One pure function per outcome so the mapping is unit-tested without a store or HTTP.
// Only a 200 whose Configured is false is NotConfigured. 401 is Unauthenticated: the endpoint
// requires the identity-session cookie and the user is signed out (or the session expired). Every
// other problem, including 403, 5xx, the transport's synthetic 499 for a cancelled request, and an
// unexpected file body, is Error with the text from Describe so the user sees what failed.
// Describe is shared with RelayChatClient so a failed completion reads the same way.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using TimeWarp.Architecture.Features.AgentChats;

/// <summary>Result of one Ask configuration probe.</summary>
/// <param name="Readiness">What Ask should render.</param>
/// <param name="SetupCommand">The user-secrets command; shown only for NotConfigured.</param>
/// <param name="Model">The configured model id when Configured.</param>
/// <param name="Problem">Status, title and detail when Error.</param>
public sealed record ChatProbeResult
(
  CatalogAgentReadiness Readiness,
  string SetupCommand,
  string? Model,
  string? Problem
);

/// <summary>Maps the Ask configuration probe's outcome to a <see cref="ChatProbeResult"/>.</summary>
public static class ChatReadinessProbe
{
  /// <summary>The status the server returns when the identity-session cookie is missing or expired.</summary>
  public const int UnauthorizedStatus = 401;

  /// <summary>The server answered. Configured or not is the server's word.</summary>
  public static ChatProbeResult FromResponse(GetAgentChatConfiguration.Response response)
  {
    ArgumentNullException.ThrowIfNull(response);
    return new ChatProbeResult
    (
      response.Configured ? CatalogAgentReadiness.Configured : CatalogAgentReadiness.NotConfigured,
      string.IsNullOrWhiteSpace(response.SetupCommand) ? XaiChatDefaults.SetupCommand : response.SetupCommand,
      response.Configured ? response.Model : null,
      null
    );
  }

  /// <summary>401 is Unauthenticated; anything else is Error with the problem text.</summary>
  public static ChatProbeResult FromProblem(SharedProblemDetails problem)
  {
    ArgumentNullException.ThrowIfNull(problem);
    return problem.Status == UnauthorizedStatus
      ? new ChatProbeResult(CatalogAgentReadiness.Unauthenticated, XaiChatDefaults.SetupCommand, null, null)
      : new ChatProbeResult(CatalogAgentReadiness.Error, XaiChatDefaults.SetupCommand, null, Describe(problem));
  }

  /// <summary>The endpoint never returns a file; treat one as a failed probe.</summary>
  public static ChatProbeResult FromFileResponse() =>
    new
    (
      CatalogAgentReadiness.Error,
      XaiChatDefaults.SetupCommand,
      null,
      "The server returned a file instead of the Ask configuration."
    );

  /// <summary>"401 Unauthorized: detail" style text for a problem. Never empty.</summary>
  public static string Describe(SharedProblemDetails problem)
  {
    ArgumentNullException.ThrowIfNull(problem);
    string status = problem.Status is { } code ? code.ToString(System.Globalization.CultureInfo.InvariantCulture) : "No status";
    string title = string.IsNullOrWhiteSpace(problem.Title) ? "Request failed" : problem.Title.Trim();
    string head = $"{status} {title}";
    return string.IsNullOrWhiteSpace(problem.Detail) || string.Equals(problem.Detail.Trim(), title, StringComparison.Ordinal)
      ? head
      : $"{head}: {problem.Detail.Trim()}";
  }
}
