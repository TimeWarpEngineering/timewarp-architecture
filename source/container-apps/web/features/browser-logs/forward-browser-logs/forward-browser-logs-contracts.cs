#region Purpose
// Endpoint-centric contract for forwarding browser console output to the web-server log pipeline.
#endregion

#region Design
// Development/Testing diagnostics: a JS hook loaded by the host page batches console errors/warnings,
// window errors and unhandled rejections and POSTs them here so they surface in the Aspire
// dashboard structured logs (category Web.Spa.Browser). JS is required (not a WASM ILoggerProvider)
// because boot failures such as a Mono assembly-load assertion happen before the .NET runtime starts.
// [EndpointAllowAnonymous]: runtime failures occur before sign-in; the handler is fail-closed outside
// Development/Testing (404) and rate limited, so the anonymous surface is dev-only.
// Nullability agrees with the validator (TWA0002/0003): Entries/Level/Source/Message are required
// (non-null, NotEmpty); PagePath is optional (null allowed, max length only).
// Levels/Sources are plain strings so the JS hook needs no enum wire format.
#endregion

namespace TimeWarp.Architecture.Features.BrowserLogs;

[ApiEndpoint]
[EndpointAllowAnonymous("Boot and runtime failures occur before sign-in; the handler is fail-closed (404) outside Development/Testing and rate limited.")]
public static partial class ForwardBrowserLogs
{
  public const int MaxEntriesPerBatch = 50;
  public const int MaxMessageLength = 4096;
  public const int MaxPagePathLength = 512;

  public static readonly IReadOnlyList<string> AllowedLevels = ["error", "warn", "info", "log", "debug"];
  public static readonly IReadOnlyList<string> AllowedSources = ["console", "window.error", "unhandledrejection"];

  [ApiRoute(RouteTemplate: "api/browser-logs", HttpVerb.Post)]
  public sealed partial class Command : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public List<Entry> Entries { get; set; } = [];
    public string? PagePath { get; set; }
  }

  public sealed class Entry
  {
    /// <summary>One of <see cref="AllowedLevels"/>.</summary>
    public string Level { get; set; } = null!;

    /// <summary>One of <see cref="AllowedSources"/>.</summary>
    public string Source { get; set; } = null!;

    public string Message { get; set; } = null!;
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(command => command.Entries)
        .NotEmpty()
        .Must(entries => entries.Count <= MaxEntriesPerBatch)
        .WithMessage($"A batch carries at most {MaxEntriesPerBatch} entries.");

      RuleForEach(command => command.Entries)
        .ChildRules(entry =>
        {
          entry.RuleFor(item => item.Level).NotEmpty().Must(level => AllowedLevels.Contains(level));
          entry.RuleFor(item => item.Source).NotEmpty().Must(source => AllowedSources.Contains(source));
          entry.RuleFor(item => item.Message).NotEmpty().MaximumLength(MaxMessageLength);
        });

      RuleFor(command => command.PagePath)
        .MaximumLength(MaxPagePathLength);
    }
  }

  public sealed class Response : BaseResponse
  {
    public int Accepted { get; init; }
  }
}
