namespace __RootNamespace__.Features.__FeatureName__s
{
  using MediatR;
  using __RootNamespace__.Features.Bases;

  public class __FeatureName__CreateRequest : BaseApiRequest, IRequest<__FeatureName__CreateResponse>
  {
    public const string RouteTemplate = "api/__FeatureName__s/__FeatureName__CreateRequest";

    /// <summary>
    /// Description of the Item
    /// </summary>
    /// <example>Super cool thing.</example>
    public string Description { get; set; }

    /// <summary>
    /// Name of the Item
    /// </summary>
    /// <example>Super Cool Item</example>
    public string Name { get; set; }

    /// <summary>
    /// Uri of image displaying item
    /// </summary>
    /// <example>https://www.gravatar.com/avatar/fb214494d2a75080e8019f5fc961a1d9</example>
    #pragma warning disable CA1056 // Uri properties should not be strings
    public string PictureUri { get; set; }
    #pragma warning restore CA1056 // Uri properties should not be strings

    /// <summary>
    /// The Price of the Item
    /// </summary>
    /// <example>999.99</example>
    public decimal Price { get; set; }

    internal override string RouteFactory => $"{RouteTemplate}";
  }
}