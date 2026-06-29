namespace TimeWarp.Foundation.Abstractions;

public interface IDateTimeService
{
  DateTime UtcNow { get; }
  DateTime NextUtcNow();
}
