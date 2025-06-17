namespace TimeWarp.Architecture.Features;

public class BaseError
{
  public BaseError(string aMessage)
  {
    Message = aMessage;
  }

  public string Message { get; set; }
}
