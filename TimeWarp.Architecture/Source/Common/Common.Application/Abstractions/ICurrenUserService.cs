namespace TimeWarp.Architecture.Abstractions;

public interface ICurrenUserService
{
  // TODO: Should this be a strongly typed UserId?
  Guid? UserId { get; }
}
