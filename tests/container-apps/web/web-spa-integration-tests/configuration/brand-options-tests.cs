#region Purpose
// Pins BrandOptions binding, the validator, /brand.css, and the Fluent startup script.
#endregion

#region Design
// No host. The stylesheet and the script are pure functions of BrandOptions. The validator is the
// same internal type ValidateOnStart runs. tokens.css is read from the repo so a default hex
// cannot drift from the fallback sheet: every declaration Render emits for new BrandOptions()
// must appear in that file. Both checked-in BrandOptions sections bind through the configuration
// binder and must equal new BrandOptions().
#endregion

namespace BrandOptions_;

using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using TimeWarp.Architecture.Configuration;

[TestTag("Unit")]
public class BrandOptions_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<BrandOptions_Should_>();

  public static Task Accept_The_Checked_In_Brand_And_Reject_A_Bad_Seed_Logo_Or_Name()
  {
    BrandOptionsValidator validator = new();
    validator.Validate(new BrandOptions()).IsValid.ShouldBeTrue();

    validator.Validate(new BrandOptions { SeedColor = "red" }).IsValid.ShouldBeFalse();
    validator.Validate(new BrandOptions { LogoSrc = "javascript:alert(1)" }).IsValid.ShouldBeFalse();
    validator.Validate(new BrandOptions { LogoSrc = "//cdn.example/mark.svg" }).IsValid.ShouldBeFalse();
    validator.Validate(new BrandOptions { ProductName = " " }).IsValid.ShouldBeFalse();
    validator.Validate(new BrandOptions { LogoHeight = 0 }).IsValid.ShouldBeFalse();
    validator.Validate(new BrandOptions { Tokens = new BrandTokenOptions { Purple = "purple" } }).IsValid.ShouldBeFalse();

    validator.Validate(new BrandOptions { LogoSrc = "https://cdn.example/mark.svg" }).IsValid.ShouldBeTrue();
    validator.Validate(new BrandOptions { LogoSrc = "https://cdn.example/mark.svg?v=1" }).IsValid.ShouldBeTrue();
    validator.Validate(new BrandOptions { LogoSrc = "https://cdn.example/mark.svg#mark" }).IsValid.ShouldBeTrue();
    validator.Validate(new BrandOptions { LogoSrc = "https://cdn.example/mark.svg?v=1#mark" }).IsValid.ShouldBeTrue();
    validator.Validate(new BrandOptions { LogoSrc = "/images/mark.svg" }).IsValid.ShouldBeTrue();
    validator.Validate(new BrandOptions { LogoSrc = "/images/mark.svg?v=1" }).IsValid.ShouldBeFalse();
    validator.Validate(new BrandOptions { LogoSrc = "images/mark.svg#mark" }).IsValid.ShouldBeFalse();
    validator.Validate(new BrandOptions { SeedColor = " #abc " }).IsValid.ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task Bind_The_BrandOptions_Section_And_Keep_Unset_Token_Defaults()
  {
    ConfigurationBuilder builder = new();
    builder.AddInMemoryCollection(new Dictionary<string, string?>
    {
      ["BrandOptions:ProductName"] = "Northwind",
      ["BrandOptions:SeedColor"] = "#abc",
      ["BrandOptions:ExactRamp"] = "true",
      ["BrandOptions:Tokens:Purple"] = "#123456"
    });

    BrandOptions bound = new();
    builder.Build().GetSection(BrandOptions.SectionName).Bind(bound);

    bound.ProductName.ShouldBe("Northwind");
    bound.NormalizedSeedColor.ShouldBe("#abc");
    bound.ExactRamp.ShouldBeTrue();
    bound.Tokens.Purple.ShouldBe("#123456");
    bound.Tokens.Blue.ShouldBe("#0085b2");
    bound.LogoSrc.ShouldBe(BrandOptions.DefaultLogoSrc);
    return Task.CompletedTask;
  }

  public static Task Render_Default_Tokens_That_Match_The_Fallback_Sheet()
  {
    string css = BrandStylesheet.Render(new BrandOptions());
    css.ShouldNotContain("<");

    string tokens = File.ReadAllText(Path.Combine(RepoRoot(), "source/container-apps/web/projects/web-spa/wwwroot/css/tokens.css"));
    int declarations = 0;
    foreach (string line in css.Split('\n'))
    {
      string declaration = line.Trim();
      if (declaration.Length == 0 || declaration is ":root {" or "}")
      {
        continue;
      }

      declaration.ShouldEndWith(";");
      tokens.ShouldContain(declaration);
      declarations++;
    }

    declarations.ShouldBe(typeof(BrandTokenOptions).GetProperties().Length + 1);
    return Task.CompletedTask;
  }

  public static Task Render_A_Custom_Token_And_Logo_Height_And_Refuse_A_Non_Hex()
  {
    BrandOptions brand = new()
    {
      LogoHeight = 64,
      Tokens = new BrandTokenOptions { Purple = "#abcdef" }
    };

    string css = BrandStylesheet.Render(brand);
    css.ShouldContain("--twe-purple: #abcdef;");
    css.ShouldContain("--twe-logo-height: 64px;");
    css.ShouldContain("--twe-blue: #0085b2;");

    Should.Throw<InvalidOperationException>(() => BrandStylesheet.Render(new BrandOptions
    {
      Tokens = new BrandTokenOptions { Purple = "nope" }
    }));
    return Task.CompletedTask;
  }

  public static Task Write_The_Fluent_Seed_And_Exact_Flag_Without_The_Product_Name()
  {
    BrandOptions brand = new()
    {
      ProductName = "Northwind",
      SeedColor = " #112233 ",
      ExactRamp = true
    };

    string script = BrandThemeScript.Render(brand);
    script.ShouldContain("existing.color = \"#112233\";");
    script.ShouldContain("existing.exact = true;");
    script.ShouldContain(BrandOptions.FluentThemeStorageKey);
    script.ShouldContain("existing.base = \"brand\";");
    script.ShouldNotContain("Northwind");
    script.ShouldNotContain("mode");
    script.ShouldNotContain("<");

    string harmonized = BrandThemeScript.Render(new BrandOptions());
    harmonized.ShouldContain("existing.color = \"#55409c\";");
    harmonized.ShouldContain("existing.exact = false;");

    Should.Throw<InvalidOperationException>(() => BrandThemeScript.Render(new BrandOptions { SeedColor = "red" }));
    return Task.CompletedTask;
  }

  public static Task Bind_The_Checked_In_Sections_To_The_Code_Defaults()
  {
    BrandOptions defaults = new();
    BrandOptions server = BindCheckedIn("source/container-apps/web/projects/web-server/appsettings.json");
    BrandOptions spa = BindCheckedIn("source/container-apps/web/projects/web-spa/wwwroot/appsettings.json");
    ShouldMatch(server, defaults);
    ShouldMatch(spa, defaults);
    ShouldMatch(server, spa);
    return Task.CompletedTask;
  }

  public static Task Revalidate_The_Sheet_With_A_Strong_ETag()
  {
    BrandStylesheet.Response first = BrandStylesheet.Respond(new BrandOptions(), ifNoneMatch: null);
    first.StatusCode.ShouldBe(200);
    first.Body.ShouldNotBeNull();
    first.Body.ShouldContain("--twe-purple: #55409c;");
    first.EntityTag.ShouldStartWith("\"");
    first.EntityTag.ShouldEndWith("\"");
    first.EntityTag.ShouldNotContain("W/");
    BrandStylesheet.Response.CacheControl.ShouldBe("no-cache");

    BrandStylesheet.Response again = BrandStylesheet.Respond(new BrandOptions(), first.EntityTag);
    again.StatusCode.ShouldBe(304);
    again.Body.ShouldBeNull();
    again.EntityTag.ShouldBe(first.EntityTag);

    BrandStylesheet.Response listed = BrandStylesheet.Respond(new BrandOptions(), "\"other\", " + first.EntityTag);
    listed.StatusCode.ShouldBe(304);

    BrandStylesheet.Response any = BrandStylesheet.Respond(new BrandOptions(), "*");
    any.StatusCode.ShouldBe(304);

    BrandStylesheet.Response weak = BrandStylesheet.Respond(new BrandOptions(), "W/" + first.EntityTag);
    weak.StatusCode.ShouldBe(304);
    weak.Body.ShouldBeNull();
    weak.EntityTag.ShouldBe(first.EntityTag);

    BrandStylesheet.Response weakListed = BrandStylesheet.Respond
    (
      new BrandOptions(),
      "\"other\", W/" + first.EntityTag
    );
    weakListed.StatusCode.ShouldBe(304);
    weakListed.Body.ShouldBeNull();

    BrandStylesheet.Response weakOther = BrandStylesheet.Respond(new BrandOptions(), "W/\"other\"");
    weakOther.StatusCode.ShouldBe(200);

    BrandOptions custom = new() { Tokens = new BrandTokenOptions { Purple = "#abcdef" } };
    BrandStylesheet.Response changed = BrandStylesheet.Respond(custom, first.EntityTag);
    changed.StatusCode.ShouldBe(200);
    changed.EntityTag.ShouldNotBe(first.EntityTag);
    return Task.CompletedTask;
  }

  private static string RepoRoot()
  {
    string? directory = AppContext.BaseDirectory;
    while (directory is not null && !File.Exists(Path.Combine(directory, "timewarp-architecture.slnx")))
    {
      directory = Path.GetDirectoryName(directory);
    }

    directory.ShouldNotBeNull();
    return directory;
  }

  private static BrandOptions BindCheckedIn(string relativePath)
  {
    using FileStream stream = File.OpenRead(Path.Combine(RepoRoot(), relativePath));
    using JsonDocument document = JsonDocument.Parse(stream);
    Dictionary<string, string?> values = new(StringComparer.OrdinalIgnoreCase);
    Flatten(BrandOptions.SectionName, document.RootElement.GetProperty(BrandOptions.SectionName), values);

    ConfigurationBuilder builder = new();
    builder.AddInMemoryCollection(values);
    BrandOptions bound = new();
    builder.Build().GetSection(BrandOptions.SectionName).Bind(bound);
    return bound;
  }

  private static void Flatten(string prefix, JsonElement element, Dictionary<string, string?> values)
  {
    if (element.ValueKind == JsonValueKind.Object)
    {
      foreach (JsonProperty property in element.EnumerateObject())
      {
        Flatten(prefix + ":" + property.Name, property.Value, values);
      }

      return;
    }

    if (element.ValueKind == JsonValueKind.Array)
    {
      int index = 0;
      foreach (JsonElement item in element.EnumerateArray())
      {
        Flatten(prefix + ":" + index.ToString(CultureInfo.InvariantCulture), item, values);
        index++;
      }

      return;
    }

    values[prefix] = Scalar(element);
  }

  private static string? Scalar(JsonElement element) =>
    element.ValueKind switch
    {
      JsonValueKind.String => element.GetString(),
      JsonValueKind.Null => null,
      JsonValueKind.True => "true",
      JsonValueKind.False => "false",
      JsonValueKind.Number => element.GetRawText(),
      _ => throw new InvalidOperationException("BrandOptions JSON contains " + element.ValueKind + ".")
    };

  private static void ShouldMatch(BrandOptions actual, BrandOptions expected)
  {
    foreach (PropertyInfo property in typeof(BrandOptions).GetProperties())
    {
      object? actualValue = property.GetValue(actual);
      object? expectedValue = property.GetValue(expected);
      if (actualValue is BrandTokenOptions actualTokens && expectedValue is BrandTokenOptions expectedTokens)
      {
        ShouldMatchTokens(actualTokens, expectedTokens);
        continue;
      }

      actualValue.ShouldBe(expectedValue, property.Name);
    }
  }

  private static void ShouldMatchTokens(BrandTokenOptions actual, BrandTokenOptions expected)
  {
    foreach (PropertyInfo property in typeof(BrandTokenOptions).GetProperties())
    {
      property.GetValue(actual).ShouldBe(property.GetValue(expected), property.Name);
    }
  }
}
