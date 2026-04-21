namespace TimeWarp.Architecture.Types;

public class FileResponse
{
  public Stream FileStream { get; }
  public string? FileName { get; init; }
  public string? ContentType { get; init; }

  public FileResponse(Stream fileStream)
  {
    FileStream = fileStream;
  }
}
