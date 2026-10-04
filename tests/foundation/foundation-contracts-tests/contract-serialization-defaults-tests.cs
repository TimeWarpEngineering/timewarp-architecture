#region Purpose
// Contract-seam options keep Enumeration out of contracts: no Enumeration converter is registered (task 105).
#endregion

namespace ContractSerializationDefaults_;

using System.Text.Json;
using System.Text.Json.Serialization;

public class Apply_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Apply_Given_>();

  public static Task Options_Should_Not_Register_An_Enumeration_Converter()
  {
    ContractSerializationDefaults.Options.Converters
      .Any(converter => converter.GetType().Name == "EnumerationJsonConverterFactory")
      .ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Options_Should_Register_The_String_Enum_Converter_Once()
  {
    JsonSerializerOptions options = new();
    ContractSerializationDefaults.Apply(options);
    ContractSerializationDefaults.Apply(options);
    options.Converters.OfType<JsonStringEnumConverter>().Count().ShouldBe(1);
    return Task.CompletedTask;
  }
}
