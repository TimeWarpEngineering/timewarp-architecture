#region Purpose
// Problem details for the chat relay. Details are safe to show; they never include upstream exceptions.
#endregion

#region Design
// Unauthenticated is defense in depth behind EndpointAuthorize. NotConfigured is a 503 so the
// client can show the setup command without treating a missing key as a crash. TooManyRequests
// is the admission cap. UpstreamFailed stays generic because provider exceptions can echo the key.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats.Application;

public static class AgentChatProblems
{
  public static SharedProblemDetails Unauthenticated() => new()
  {
    Title = "Unauthenticated",
    Status = 401,
    Detail = "No authenticated principal."
  };

  public static SharedProblemDetails NotConfigured() => new()
  {
    Title = "AI not configured",
    Status = 503,
    Detail = XaiChatDefaults.SetupCommand
  };

  public static SharedProblemDetails TooManyRequests() => new()
  {
    Title = "Too many requests",
    Status = 429,
    Detail = "Too many model requests. Wait a moment and try again."
  };

  public static SharedProblemDetails UpstreamFailed() => new()
  {
    Title = "Model request failed",
    Status = 502,
    Detail = "The model request failed."
  };

  public static SharedProblemDetails EmptyModelResponse() => new()
  {
    Title = "Empty model response",
    Status = 502,
    Detail = "The model returned an empty response."
  };
}
