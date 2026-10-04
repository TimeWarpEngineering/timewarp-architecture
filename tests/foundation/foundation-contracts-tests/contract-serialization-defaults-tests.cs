#region Purpose
// Contract-seam round-trips for Enumeration members through ContractSerializationDefaults (task 105).
#endregion

namespace ContractSerializationDefaults_;

using System.Text.Json;
using TimeWarp.Foundation.Enumerations;

/// <summary>A concrete <see cref="Enumeration"/> as it would appear in a contract.</summary>
internal sealed class Priority : Enumeration
{
  public static readonly Priority Low = new(1, "Low");
  public static readonly Priority High = new(2, "High");

  private Priority(int value, string name) : base(value, name, null) { }
}

/// <summary>Contract-shaped response carrying an Enumeration member.</summary>
internal sealed record TicketResponse(string Title, Priority Priority, Priority? Escalation);

public class Enumeration_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Enumeration_Given_>();

  public static Task Contract_Options_Should_Round_Trip_By_Name()
  {
    TicketResponse response = new("Outage", Priority.High, null);

    string json = JsonSerializer.Serialize(response, ContractSerializationDefaults.Options);
    json.ShouldBe("""{"title":"Outage","priority":"High","escalation":null}""");

    TicketResponse roundTripped = JsonSerializer.Deserialize<TicketResponse>(json, ContractSerializationDefaults.Options)!;
    roundTripped.ShouldBe(response);
    roundTripped.Priority.ShouldBeSameAs(Priority.High);
    return Task.CompletedTask;
  }

  public static Task Unknown_Name_Should_Fail_Closed()
  {
    Should.Throw<JsonException>
    (
      () => JsonSerializer.Deserialize<TicketResponse>("""{"title":"x","priority":"Urgent"}""", ContractSerializationDefaults.Options)
    );
    return Task.CompletedTask;
  }

  public static Task Integer_Value_Should_Fail_Closed()
  {
    Should.Throw<JsonException>
    (
      () => JsonSerializer.Deserialize<TicketResponse>("""{"title":"x","priority":2}""", ContractSerializationDefaults.Options)
    );
    return Task.CompletedTask;
  }

  public static Task Apply_Should_Not_Register_The_Factory_Twice()
  {
    JsonSerializerOptions options = new();
    ContractSerializationDefaults.Apply(options);
    ContractSerializationDefaults.Apply(options);
    options.Converters.OfType<EnumerationJsonConverterFactory>().Count().ShouldBe(1);
    return Task.CompletedTask;
  }
}
