namespace TimeWarp.Architecture.Abstractions;

public interface IDateTimeService
{
  DateTime UtcNow { get; }
  DateTime NextUtcNow();
}
