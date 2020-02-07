namespace TimeWarp.Blazor.BookFeature
{
  using MediatR;
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;
  using System.Threading;
  using System.Threading.Tasks;

  public class GetBooksHandler : IRequestHandler<GetBooksRequest, GetBooksResponse>
  {
    public int MyProperty { get; set; }
    public Task<GetBooksResponse> Handle(GetBooksRequest aGetBooksRequest, CancellationToken aCancellationToken)
    {
      var getBooksResponse = new GetBooksResponse(aGetBooksRequest.Id);


    }
  }
}
