namespace TimeWarp.Blazor.Models
{
  using MongoDB.Bson;
  using MongoDB.Bson.Serialization.Attributes;

  public class Book : BaseModel
  {

    [BsonElement("Name")]
    public string BookName { get; set; }

    public decimal Price { get; set; }

    public string Category { get; set; }

    public string Author { get; set; }
  }
}