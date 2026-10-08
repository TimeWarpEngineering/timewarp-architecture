#region Purpose
// Builds the JSON schema a tool caller sees for one catalog entry.
#endregion

#region Design
// Each constructor parameter becomes one property. The catalog's JsonSchema is used when the
// generator emitted one (primitives). A complex parameter is one object level of public
// properties, camelCase, because v1 of the catalog stores only the CLR type name. UserId is
// left out: handlers stamp that mock-mode auth signal and it is not a tool argument.
// Follow-up is a timewarp-state schema for complex parameters (design.md item 1).
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using System.Reflection;
using System.Text.Json.Nodes;

public static class CatalogAgentSchema
{
  public static string For(ActionCatalogEntry entry)
  {
    ArgumentNullException.ThrowIfNull(entry);
    JsonObject properties = [];
    JsonArray required = [];

    foreach (ActionCatalogParameter parameter in entry.Parameters)
    {
      properties[parameter.Name] = PropertySchema(parameter);
      if (parameter.IsRequired)
      {
        required.Add(parameter.Name);
      }
    }

    JsonObject schema = new()
    {
      ["type"] = "object",
      ["properties"] = properties,
      ["additionalProperties"] = false,
    };

    if (required.Count > 0)
    {
      schema["required"] = required;
    }

    return schema.ToJsonString();
  }

  private static JsonNode PropertySchema(ActionCatalogParameter parameter)
  {
    if (!string.IsNullOrWhiteSpace(parameter.JsonSchema))
    {
      return JsonNode.Parse(parameter.JsonSchema) ?? new JsonObject { ["type"] = "string" };
    }

    Type type = Nullable.GetUnderlyingType(parameter.ClrType) ?? parameter.ClrType;
    if (TryPrimitiveType(type, out string? jsonType))
    {
      return new JsonObject { ["type"] = jsonType };
    }

    if (type.IsEnum)
    {
      JsonArray names = [];
      foreach (string name in Enum.GetNames(type))
      {
        names.Add(name);
      }

      return new JsonObject { ["type"] = "string", ["enum"] = names };
    }

    return ComplexSchema(type);
  }

  private static JsonObject ComplexSchema(Type type)
  {
    JsonObject properties = [];
    JsonArray required = [];
    NullabilityInfoContext nullability = new();

    foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
    {
      if (property.GetMethod is null || property.GetMethod.GetParameters().Length > 0)
      {
        continue;
      }

      if (property.Name == "UserId")
      {
        continue;
      }

      Type propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
      JsonObject node = TryPrimitiveType(propertyType, out string? jsonType)
        ? new JsonObject { ["type"] = jsonType }
        : new JsonObject { ["type"] = "string", ["description"] = propertyType.Name };

      properties[JsonNamingPolicy.CamelCase.ConvertName(property.Name)] = node;

      bool nullableReference = !propertyType.IsValueType
        && nullability.Create(property).WriteState == NullabilityState.Nullable;
      if (!nullableReference && Nullable.GetUnderlyingType(property.PropertyType) is null)
      {
        required.Add(JsonNamingPolicy.CamelCase.ConvertName(property.Name));
      }
    }

    JsonObject schema = new()
    {
      ["type"] = "object",
      ["properties"] = properties,
      ["additionalProperties"] = false,
    };

    if (required.Count > 0)
    {
      schema["required"] = required;
    }

    return schema;
  }

  private static bool TryPrimitiveType(Type type, out string jsonType)
  {
    if (type == typeof(string) || type == typeof(Guid) || type == typeof(DateTime)
      || type == typeof(DateTimeOffset) || type == typeof(DateOnly) || type == typeof(TimeOnly)
      || type == typeof(TimeSpan) || type == typeof(char))
    {
      jsonType = "string";
      return true;
    }

    if (type == typeof(bool))
    {
      jsonType = "boolean";
      return true;
    }

    if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
    {
      jsonType = "number";
      return true;
    }

    if (type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort)
      || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong))
    {
      jsonType = "integer";
      return true;
    }

    jsonType = "";
    return false;
  }
}
