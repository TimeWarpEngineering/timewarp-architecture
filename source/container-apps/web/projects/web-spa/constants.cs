#region Purpose
// SPA-local constants that have no home elsewhere, such as the non-standard HTTP status representing a client-cancelled operation.
#endregion

namespace TimeWarp.Architecture;

internal static class Constants
{
  public const int OperationCancelled =  499; // 499 is the code for "Client Closed Request" used by Nginx

}
