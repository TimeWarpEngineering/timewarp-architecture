namespace TimeWarp.Architecture.Features.Profiles.Application;
//<SolutionName>.<ContainerName>.Features.<FeatureName>.<Layer>
//<SolutionName>.Features.<FeatureName>.<Layer>
using static TimeWarp.Architecture.Features.Profiles.GetProfile;

public class GetProfile
{
  // TODO: Finish implementation
  public class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    private readonly ICurrenUserService CurrenUserService;
    private readonly HttpClient HttpClient;
    private readonly ILogger<Handler> Logger;
    public Handler
    (
      ICurrenUserService currenUserService,
      HttpClient httpClient,
      ILogger<Handler> logger
    )
    {
      CurrenUserService = currenUserService;
      HttpClient = httpClient;
      Logger = logger;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(Query request, CancellationToken cancellationToken)
    {
      MockResponseFactory<Response> mockResponseFactory = GetMockResponseFactory();
      Response response = mockResponseFactory(request);
      // https://github.com/kesac/Syllabore

      Guid? userId = CurrenUserService.UserId;
      if (userId is null) return response;
      // TODO: Read the Profile from the DB/Repository/Service
      // The ProfileId will be the UserId.

      response =
        new Response
        (
          alias: "Use Syllabore",
          avatar: await GetAvatarDataUri(userId.Value)
        )
      ;

      return response;
    }


    // TODO: This will be moved to where we register a user and stored in the DB
    // By storing in the DB our frontend won't be calling out to mulitavatar.com and leaking infomration.
    // Only the backend will do it once and then store it.
    private async Task<string> GetAvatarDataUri(Guid userId)
    {
      // string avatarUrl = "https://api.multiavatar.com/d8f42d42-f2f8-4332-af82-8ff357f61aa5.svg";
      string avatarUrl = $"https://api.multiavatar.com/{userId}.svg";
      byte[] imageBytes = await HttpClient.GetByteArrayAsync(avatarUrl);
      string base64 = Convert.ToBase64String(imageBytes);
      return $"data:image/svg+xml;base64,{base64}";
    }
  }
}
