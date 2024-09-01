namespace TimeWarp.Architecture.Web.Application.Features.Profile;

using static TimeWarp.Architecture.Features.Profiles.GetProfile;


public class GetProfile
{
  // TODO: Finish implementation
  public class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    private readonly HttpClient HttpClient;
    private readonly ILogger<Handler> Logger;
    public Handler(HttpClient httpClient, ILogger<Handler> logger)
    {
      HttpClient = httpClient;
      Logger = logger;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(Query request, CancellationToken cancellationToken)
    {
      // https://github.com/kesac/Syllabore
      var response =
        new Response
        (
          alias:"Use Syllabore",
          avatar: await GetAvatarDataUri()
        )
      ;

      return response;
    }

    private async Task<string> GetAvatarDataUri()
    {
      string avatarUrl = "https://multiavatar.com/d8f42d42-f2f8-4332-af82-8ff357f61aa5.svg";
      // var avatarUrl = $"https://multiavatar.com/{PasskeyId}.svg";
      byte[] imageBytes = await HttpClient.GetByteArrayAsync(avatarUrl);
      string base64 = Convert.ToBase64String(imageBytes);
      return $"data:image/svg+xml;base64,{base64}";
    }
  }
}
