namespace TimeWarp.Foundation.Types;

public delegate TResponse MockResponseFactory<out TResponse>(IApiRequest request) where TResponse : class;
