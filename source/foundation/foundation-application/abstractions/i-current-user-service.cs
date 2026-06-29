namespace TimeWarp.Foundation.Abstractions;

public interface ICurrentUserService
{
  // TODO: Should this be a strongly typed UserId?
  Guid? UserId { get; }
}
