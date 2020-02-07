namespace TimeWarp.Blazor.Models
{
  using MongoDB.Bson;
  using MongoDB.Bson.Serialization.Attributes;
  using System;

  public class BaseModel
  {
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public Guid Id { get; set; }

  }
}
