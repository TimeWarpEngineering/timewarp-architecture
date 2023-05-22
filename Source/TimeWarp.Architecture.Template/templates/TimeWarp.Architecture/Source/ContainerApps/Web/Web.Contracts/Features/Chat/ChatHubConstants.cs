namespace TimeWarp.Architecture.Features.Chat.Contracts;

using Microsoft.AspNetCore.SignalR;

public static class ChatHubConstants
{
  public const string Route = "/chat-hub";
}

public interface IChatHubClients : IHubClients { };
