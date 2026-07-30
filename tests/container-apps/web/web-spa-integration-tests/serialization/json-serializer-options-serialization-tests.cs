namespace JsonSerializerOptions_;

public class JsonSerializer_Should
{
  public void SerializeAndDeserializePerson()
  {
    JsonSerializerOptions jsonSerializerOptions = ContractSerializationDefaults.Options;
    var person = new Person { FirstName = "Steve", LastName = "Cramer", BirthDay = new DateTime(1967, 09, 27) };
    string json = JsonSerializer.Serialize(person, jsonSerializerOptions);
    Person parsed = JsonSerializer.Deserialize<Person>(json, jsonSerializerOptions)!;
    parsed.BirthDay.ShouldBe(person.BirthDay);
    parsed.FirstName.ShouldBe(person.FirstName);
    parsed.LastName.ShouldBe(person.LastName);
  }
}
