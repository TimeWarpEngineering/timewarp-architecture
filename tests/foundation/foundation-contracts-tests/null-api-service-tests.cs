#region Purpose
// Verifies NullApiService returns a 501 problem arm and never throws for missing transport.
#endregion

namespace TimeWarp.Architecture.Foundation.Contracts.Tests;

using System.Net;
using System.Threading;
using System.Threading.Tasks;
using OneOf;

public class NullApiService_GetResponse
{
  private sealed class SampleRequest : IApiRequest
  {
    public string GetRoute() => "/api/sample";
    public HttpVerb GetHttpVerb() => HttpVerb.Get;
  }

  private sealed class ThrowingRouteRequest : IApiRequest
  {
    public string GetRoute() => throw new InvalidOperationException("route broken");
    public HttpVerb GetHttpVerb() => HttpVerb.Post;
  }

  public async Task Returns_problem_arm_with_501_and_does_not_throw()
  {
    NullApiService service = new();
    SampleRequest request = new();

    OneOf<object, FileResponse, SharedProblemDetails> result =
      await service.GetResponse<object>(request, CancellationToken.None);

    result.IsT2.ShouldBeTrue();
    SharedProblemDetails problem = result.AsT2;
    problem.Status.ShouldBe((int)HttpStatusCode.NotImplemented);
    problem.Title.ShouldBe("No API backend");
    problem.Detail.ShouldNotBeNull();
    problem.Detail.ShouldContain(typeof(SampleRequest).FullName!);
    problem.Detail.ShouldContain("Get");
    problem.Detail.ShouldContain("/api/sample");
  }

  public async Task Returns_problem_arm_when_route_metadata_throws()
  {
    NullApiService service = new();
    ThrowingRouteRequest request = new();

    OneOf<object, FileResponse, SharedProblemDetails> result =
      await service.GetResponse<object>(request, CancellationToken.None);

    result.IsT2.ShouldBeTrue();
    SharedProblemDetails problem = result.AsT2;
    problem.Status.ShouldBe((int)HttpStatusCode.NotImplemented);
    problem.Detail.ShouldNotBeNull();
    problem.Detail!.ShouldContain(typeof(ThrowingRouteRequest).FullName!);
    problem.Detail.ShouldContain("route unavailable");
  }
}
