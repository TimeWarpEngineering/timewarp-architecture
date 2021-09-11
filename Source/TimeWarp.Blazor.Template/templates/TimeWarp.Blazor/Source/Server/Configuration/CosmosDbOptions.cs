namespace TimeWarp.Blazor.Configuration
{
  public class CosmosDbOptions
  {
    /// <summary>
    /// The Cosmos DB Endpoint.
    /// </summary>
    /// <remarks>Find this value in Azure Portal by selecting your Azure Cosmos DB account and then under "Settings" select Keys</remarks>
    public string EndPoint { get; set; }

    /// <summary>
    /// The Cosmos DB access key.
    /// </summary>
    /// /// <remarks>Find this value in Azure Portal by selecting your Azure Cosmos DB account and then under "Settings" select Keys</remarks>
    public string AccessKey { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether startup should check for migrations.
    /// </summary>
    public bool EnableMigration { get; set; }

    /// <summary>
    /// Gets or sets the id of the document to check for migration.
    /// </summary>
    public string DocumentToCheck { get; set; }
  }
}
