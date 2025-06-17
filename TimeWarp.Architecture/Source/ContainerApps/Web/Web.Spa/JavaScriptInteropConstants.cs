namespace TimeWarp.Architecture;

/// <summary>
/// Contains constants for JavaScript interop functions.
/// </summary>
public static class JavaScriptInteropConstants
{
  /// <summary>
  /// The name of the JavaScript function to download a file from a stream.
  /// This constant must match the function name defined in your JavaScript file.
  /// JavaScript file location: wwwroot/js/downloadFile.js
  /// </summary>
  public const string DownloadFileFromStreamFunction = "downloadFileFromStream";
}
