// Eval fixture: generic minimal-API style — should NOT route to web-api-contracts.
namespace MyApp.Api;

public sealed record CreateProductRequest
{
  public required string Name { get; init; }
  public required decimal Price { get; init; }
}

public sealed record ProductResponse(int Id, string Name, decimal Price);