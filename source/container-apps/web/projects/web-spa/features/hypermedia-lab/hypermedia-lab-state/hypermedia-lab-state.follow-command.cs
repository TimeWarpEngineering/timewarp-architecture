#region Purpose
// FollowCommand: approach C's generic interpreter — send an offered link command, then follow the payload's Self link for the new state.
#endregion

#region Design
// The handler knows nothing about credentials. It fails closed, in this order, with a shell
// notification and no request sent:
//   1. the href is not app-relative (AppRelativeHref — never send the bearer token or the browser
//      to another origin);
//   2. the command is not in the CURRENT payload (method + href match) — a stale or hand-built
//      command cannot run;
//   3. the user's fields are not exactly the command's Fields, or one is blank;
//   4. the method is not one the lab speaks (POST, NAVIGATE).
// NAVIGATE is a full-page navigation (forceLoad) — the Entra link challenge is a browser redirect.
// POST sends FollowedLinkRequest through IWebServerApiService (same auth pipeline as typed calls):
// body = the command's Body template plus the user's fields. A problem is published as
// ProblemDetailsNotification; a 2xx (or 204) publishes "<Label>: done.". Either way the handler then
// GETs Self and replaces the payload — the server's follow-up decides the next menu (revoke down to
// one credential and Revoke disappears). Both requests run inside this one handler, so no action
// dispatches another (TWS0002).
// Cataloged (Human) only so a Ctrl-K contextual row can name it; its required parameters keep it
// out of the static roster. Fields is required (pass an empty map when the command has none):
// TimeWarp.State 12.0.0-beta.8's ActionSet method generator drops the "?" of a nullable generic
// parameter with a null default (CS8625 in the generated FollowCommand method). The success body is ignored: the existing endpoints answer "{}".
#endregion

namespace TimeWarp.Architecture.Features.HypermediaLab;

using static GetCredentialCommands;

partial class HypermediaLabState
{
  public static class FollowCommandActionSet
  {
    public const string CatalogName = "HypermediaLab.FollowCommand";

    [CatalogAction
    (
      DisplayName = "Follow link command",
      Description = "Send a link command the hypermedia lab's current payload offers (approach C).",
      Permissions = [PermissionIds.CredentialManageSelf],
      Visibility = ActionVisibility.Human
    )]
    [TrackAction]
    public sealed class Action : IBaseAction
    {
      public Action(string method, string href, IReadOnlyDictionary<string, string> fields)
      {
        Method = method;
        Href = href;
        Fields = fields;
      }

      public string Method { get; }
      public string Href { get; }
      public IReadOnlyDictionary<string, string> Fields { get; }
    }

    internal sealed class Handler
    (
      IStore store,
      TimeWarp.Architecture.Services.IWebServerApiService webServerApiService,
      NavigationManager navigationManager,
      IPublisher<ClientPipeline> publisher
    ) : BaseHandler<Action>(store)
    {
      public override async ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        if (!AppRelativeHref.IsAppRelative(action.Href))
        {
          await RefuseAsync($"Refused: '{action.Href}' is not an app-relative link.", cancellationToken);
          return;
        }

        Response? payload = HypermediaLabState.Commands;
        LinkCommand? command = payload?.Commands.FirstOrDefault
        (
          candidate => candidate.Method == action.Method && candidate.Href == action.Href
        );
        if (payload is null || command is null)
        {
          await RefuseAsync("That command is not offered now.", cancellationToken);
          return;
        }

        IReadOnlyDictionary<string, string> fields = action.Fields;
        if (!fields.Keys.Order(StringComparer.Ordinal).SequenceEqual(command.Fields.Order(StringComparer.Ordinal), StringComparer.Ordinal)
            || fields.Values.Any(string.IsNullOrWhiteSpace))
        {
          await RefuseAsync($"{command.Label} needs: {string.Join(", ", command.Fields)}.", cancellationToken);
          return;
        }

        switch (command.Method)
        {
          case LinkCommandMethods.Navigate:
            navigationManager.NavigateTo(command.Href, forceLoad: true);
            return;

          case LinkCommandMethods.Post:
            await PostAsync(command, fields, cancellationToken);
            await RefreshAsync(payload.Self, cancellationToken);
            return;

          default:
            await RefuseAsync($"Refused: method '{command.Method}' is not supported.", cancellationToken);
            return;
        }
      }

      private async Task PostAsync(LinkCommand command, IReadOnlyDictionary<string, string> fields, CancellationToken cancellationToken)
      {
        Dictionary<string, JsonElement> body = command.Body is null ? [] : new(command.Body);
        foreach ((string name, string value) in fields)
        {
          body[name] = JsonSerializer.SerializeToElement(value);
        }

        OneOf<Ignored, FileResponse, SharedProblemDetails> result =
          await webServerApiService.GetResponse<Ignored>(new FollowedLinkRequest(HttpVerb.Post, command.Href, body), cancellationToken);

        if (result.TryPickT2(out SharedProblemDetails? problem, out _) && problem.Status != (int)System.Net.HttpStatusCode.NoContent)
        {
          await publisher.Publish(new ProblemDetailsNotification(problem), cancellationToken);
          return;
        }

        await publisher.Publish(new OutcomeNotification(MessageBarIntent.Success, $"{command.Label}: done."), cancellationToken);
      }

      private async Task RefreshAsync(string self, CancellationToken cancellationToken)
      {
        if (!AppRelativeHref.IsAppRelative(self))
        {
          await RefuseAsync($"Refused: Self '{self}' is not an app-relative link.", cancellationToken);
          return;
        }

        OneOf<Response, FileResponse, SharedProblemDetails> refreshed =
          await webServerApiService.GetResponse<Response>(new FollowedLinkRequest(HttpVerb.Get, self), cancellationToken);
        if (refreshed.TryPickT0(out Response? response, out OneOf<FileResponse, SharedProblemDetails> other))
        {
          HypermediaLabState.Commands = response;
          return;
        }

        if (other.TryPickT1(out SharedProblemDetails? problem, out _))
        {
          await publisher.Publish(new ProblemDetailsNotification(problem), cancellationToken);
        }
      }

      private Task RefuseAsync(string title, CancellationToken cancellationToken) =>
        publisher.Publish(new OutcomeNotification(MessageBarIntent.Warning, title), cancellationToken);
    }

    /// <summary>Success body placeholder: the followed endpoints' JSON is not read.</summary>
    internal sealed class Ignored;
  }
}
