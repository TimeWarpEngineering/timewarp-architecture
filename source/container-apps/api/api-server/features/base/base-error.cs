namespace TimeWarp.Architecture.Features;

public class BaseError
{
  public BaseError(string message)
  {
    Message = message;
  }

  public string Message { get; set; }
}
