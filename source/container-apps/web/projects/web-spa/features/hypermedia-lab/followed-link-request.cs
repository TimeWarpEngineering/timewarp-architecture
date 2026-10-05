#region Purpose
// The one untyped IApiRequest approach C needs: a method + app-relative href + JSON body taken from a server link command.
#endregion

#region Design
// Built only by FollowCommand, after AppRelativeHref accepted the href. It travels through the same
// IWebServerApiService as every typed contract — so the same HttpClient, bearer token acquisition,
// problem-details mapping and (in mock mode) the MockWebApiService fall-through apply; there is no
// second HTTP path. HttpApiService serialises the request object itself as the body, so the body
// is a [JsonExtensionData] dictionary (its entries become the top-level JSON properties) and Verb /
// Href are [JsonIgnore] (a private [JsonConstructor] lets it deserialize). Not a routed contract (no [ApiRoute]): it names no endpoint of its own.
#endregion

namespace TimeWarp.Architecture.Features.HypermediaLab;

using System.Text.Json.Serialization;

public sealed class FollowedLinkRequest : IApiRequest
{
  public FollowedLinkRequest(HttpVerb verb, string href, Dictionary<string, JsonElement>? body = null)
  {
    Verb = verb;
    Href = href;
    Body = body ?? [];
  }

  // Deserialization only (the store round-trips dispatched requests through JSON): extension data
  // cannot bind to a constructor parameter, and Verb / Href are not on the wire.
  [JsonConstructor]
  private FollowedLinkRequest() : this(default, "") { }

  [JsonIgnore]
  public HttpVerb Verb { get; }

  [JsonIgnore]
  public string Href { get; }

  [JsonExtensionData]
  public Dictionary<string, JsonElement> Body { get; }

  public string GetRoute() => Href;

  public HttpVerb GetHttpVerb() => Verb;
}
