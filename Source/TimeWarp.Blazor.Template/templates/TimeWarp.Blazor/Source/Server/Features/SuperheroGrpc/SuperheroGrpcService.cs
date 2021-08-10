namespace TimeWarp.Blazor.Features.SuperheroGrpc
{
  using Newtonsoft.Json;
  using ProtoBuf.Grpc;
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Net.Http;
  using System.Threading.Tasks;

  public class SuperheroGrpcService : ISuperheroService
  {
    private readonly HttpClient HttpClient;
    public SuperheroGrpcService(HttpClient aHttpClient)
    {
      HttpClient = aHttpClient;
    }
    public List<int> SuperheroIds = new List<int>();
    public Task<SuperheroGrpcResponse> GetSuperheroAsync
    (
      SuperheroGrpcRequest aSuperheroGrpcRequest,
      CallContext aCallContext = default
    )
    {
      var superheroDto = new SuperheroGrpcDto() { Id = "1", Name = "Mike" };
      var listsup = new List<SuperheroGrpcDto>();
      listsup.Add(superheroDto);
      //for (int number = 1; number < aSuperheroGrpcRequest.NumberOfHero; number++)
      //{
      //  SuperheroIds.Add(number);
      //}
      var response = new SuperheroGrpcResponse()
      {
        SuperherosGrpc = listsup
      };
      //foreach (int superheroId in SuperheroIds)
      //{
      //  var builder = new UriBuilder($"https://www.superheroapi.com/api.php/2885189161696978/{0}", superheroId.ToString());
      //  HttpResponseMessage result = HttpClient.GetAsync(builder.Uri).Result;
      //  var superhero = new SuperheroGrpcDto();
      //  string content = await result.Content.ReadAsStringAsync();
      //  Console.WriteLine("SuperheroAsString" + content);
      //  superhero = JsonConvert.DeserializeObject<SuperheroGrpcDto>(content);
      //  response.SuperherosGrpc.Add(superhero);
      //}
      return Task.FromResult(response);
    }
  }
}
