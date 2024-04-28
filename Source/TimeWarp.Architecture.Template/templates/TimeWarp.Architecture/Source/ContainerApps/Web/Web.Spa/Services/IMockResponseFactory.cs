namespace TimeWarp.Architecture.Services;

public interface IMockResponseFactory
{
  object CreateMockResponse(dynamic request);
}
