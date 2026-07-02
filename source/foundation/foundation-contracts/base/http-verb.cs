#region Purpose
// Enum of HTTP methods so contracts can declare their verb without depending on System.Net.Http's HttpMethod class.
#endregion

namespace TimeWarp.Foundation.Features;
public enum HttpVerb
{
  Get,
  Post,
  Delete,
  Put,
  Patch,
  Head,
  Options
}
