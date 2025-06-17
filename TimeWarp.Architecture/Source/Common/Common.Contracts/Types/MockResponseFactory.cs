namespace TimeWarp.Architecture.Types;

public delegate TResponse MockResponseFactory<out TResponse>(IApiRequest request) where TResponse : class;
