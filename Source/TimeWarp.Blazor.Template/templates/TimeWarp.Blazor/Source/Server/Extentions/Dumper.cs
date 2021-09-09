namespace TimeWarp.Blazor.Extensions
{
  using System.Text.Json;
  using System;

  public static class Dumper
  {
    public static string ToPrettyString(this object value)
    {
      var jsonSerializerOptions = new JsonSerializerOptions
      {
        WriteIndented = true,
        MaxDepth = 4
      };
      return JsonSerializer.Serialize(value, jsonSerializerOptions);
    }

    public static T Dump<T>(this T value)
    {
      Console.WriteLine(value.ToPrettyString());
      return value;
    }
  }
}

