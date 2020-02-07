namespace TimeWarp.Blazor.BookFeature
{
  using System;
  using System.Collections.Generic;
  using TimeWarp.Blazor.Api.Features.Base;

  public class GetBooksResponse : BaseResponse
  {
    public List<BookDto> Books { get; set; }

    /// <summary>
    /// a default constructor is required for deserialization
    /// </summary>
    public GetBooksResponse() { }

    public GetBooksResponse(Guid aRequestId)
    {
      Books = new List<BookDto>();
      RequestId = aRequestId;
    }
  }
}
