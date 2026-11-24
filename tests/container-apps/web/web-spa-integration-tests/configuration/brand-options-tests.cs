#region Purpose
// Pins BrandOptions binding, the validator, /brand.css, /brand.json, /manifest.json, and the
// Fluent startup script.
#endregion

#region Design
// No host. The documents and the script are pure functions of BrandOptions. The validator is the
// same internal type ValidateOnStart runs. tokens.css is read from the repo so a default hex
// cannot drift from the fallback sheet: every declaration Render emits for new BrandOptions()
// resolves (through tokens.css var() references) to the same value in the same block.
// Task 513: web-server configuration is the one BrandOptions source, so there is no checked-in
// copy to compare. /brand.json is bound back through the configuration binder with every property
// set off its default, so a property BrandDocument does not write fails the round trip.
// The role-token guard for component CSS is BrandRoleTokens_Should_.
#endregion

namespace BrandOptions_;

using System.Reflection;
using Microsoft.Extensions.Configuration;
using TimeWarp.Architecture.Configuration;

[TestTag("Unit")]
public class BrandOptions_Should_
{
  private const string TokensCssPath = "source/container-apps/web/projects/web-spa/wwwroot/css/tokens.css";

  private const string HuePattern = "^--twe-(purple|blue|green)";

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
    validator.Validate(new BrandOptions { Tokens = new BrandTokenOptions { Primary = "purple" } }).IsValid.ShouldBeFalse();
    validator.Validate(new BrandOptions { Tokens = new BrandTokenOptions { PrimaryDark = "light" } }).IsValid.ShouldBeFalse();
    validator.Validate(new BrandOptions { FaviconSrc = "javascript:alert(1)" }).IsValid.ShouldBeFalse();
    validator.Validate(new BrandOptions { AppleTouchIconSrc = "" }).IsValid.ShouldBeFalse();
    validator.Validate(new BrandOptions { Icon192Src = "//cdn.example/icon.png" }).IsValid.ShouldBeFalse();
    validator.Validate(new BrandOptions { Icon512Src = "../icon.png" }).IsValid.ShouldBeFalse();
    validator.Validate(new BrandOptions { OgImageSrc = "data:image/png;base64,AA" }).IsValid.ShouldBeFalse();
    validator.Validate(new BrandOptions { ThemeColor = "teal" }).IsValid.ShouldBeFalse();
    validator.Validate(new BrandOptions { BackgroundColor = "#12" }).IsValid.ShouldBeFalse();
    validator.Validate(new BrandOptions { LogoAlt = "<b>" }).IsValid.ShouldBeFalse();
    validator.Validate(new BrandOptions { FaviconSrc = "images/brand/favicon.svg" }).IsValid.ShouldBeTrue();

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
      ["BrandOptions:Tokens:Primary"] = "#123456"
    });

    BrandOptions bound = new();
    builder.Build().GetSection(BrandOptions.SectionName).Bind(bound);

    bound.ProductName.ShouldBe("Northwind");
    bound.NormalizedSeedColor.ShouldBe("#abc");
    bound.ExactRamp.ShouldBeTrue();
    bound.Tokens.Primary.ShouldBe("#123456");
    bound.Tokens.Secondary.ShouldBe("#0085b2");
    bound.LogoSrc.ShouldBe(BrandOptions.DefaultLogoSrc);
    bound.FaviconSrc.ShouldBe(BrandOptions.DefaultFaviconSrc);
    return Task.CompletedTask;
  }

  public static Task Fall_Back_LogoAlt_ThemeColor_And_BackgroundColor()
  {
    BrandOptions brand = new() { ProductName = "Northwind", SeedColor = " #0a7d5a " };
    brand.EffectiveLogoAlt.ShouldBe("Northwind");
    brand.EffectiveThemeColor.ShouldBe("#0a7d5a");
    brand.EffectiveBackgroundColor.ShouldBe(BrandOptions.DefaultBackgroundColor);

    brand.LogoAlt = "Northwind Traders";
    brand.ThemeColor = "#112233";
    brand.BackgroundColor = "#ffffff";
    brand.EffectiveLogoAlt.ShouldBe("Northwind Traders");
    brand.EffectiveThemeColor.ShouldBe("#112233");
    brand.EffectiveBackgroundColor.ShouldBe("#ffffff");

    // Bound through configuration, a later ProductName or seed still reaches the fallbacks.
    BrandOptions bound = new();
    new ConfigurationBuilder().Build().GetSection(BrandOptions.SectionName).Bind(bound);
    bound.ProductName = "Contoso";
    bound.SeedColor = "#123456";
    bound.EffectiveLogoAlt.ShouldBe("Contoso");
    bound.EffectiveThemeColor.ShouldBe("#123456");

    string tokens = ReadRepoFile(TokensCssPath);
    TokenBlock(tokens, ":root")["--twe-background"].ShouldBe(BrandOptions.DefaultBackgroundColor);
    return Task.CompletedTask;
  }

  public static Task Render_Default_Tokens_That_Match_The_Fallback_Sheet()
  {
    string css = BrandStylesheet.Render(new BrandOptions());
    css.ShouldNotContain("<");

    string tokens = ReadRepoFile(TokensCssPath);
    Dictionary<string, string> light = TokenBlock(tokens, ":root");
    Dictionary<string, string> dark = TokenBlock(tokens, BrandStylesheet.DarkSelector);
    int declarations = 0;
    Dictionary<string, string>? block = null;
    foreach (string line in css.Split('\n'))
    {
      string declaration = line.Trim();
      if (declaration.Length == 0 || declaration == "}")
      {
        continue;
      }

      if (declaration == ":root {")
      {
        block = light;
        continue;
      }

      if (declaration == BrandStylesheet.DarkSelector + " {")
      {
        block = dark;
        continue;
      }

      block.ShouldNotBeNull(declaration);
      declaration.ShouldEndWith(";");
      int colon = declaration.IndexOf(':', StringComparison.Ordinal);
      string name = declaration[..colon];
      string value = declaration[(colon + 1)..^1].Trim();
      Resolve(name, block, light).ShouldBe(value, name);
      name.ShouldNotMatch(HuePattern);
      declarations++;
    }

    // One per BrandTokenOptions property plus --twe-logo-height.
    declarations.ShouldBe(typeof(BrandTokenOptions).GetProperties().Length + 1);
    return Task.CompletedTask;
  }

  public static Task Write_The_Dark_Role_Variants_In_The_Dark_Block()
  {
    BrandOptions brand = new()
    {
      Tokens = new BrandTokenOptions { PrimaryDark = "#ffd0a8", SecondaryDark = "#9fe7ff", HighlightDark = "#b8f28d" }
    };

    string css = BrandStylesheet.Render(brand);
    int dark = css.IndexOf(BrandStylesheet.DarkSelector + " {", StringComparison.Ordinal);
    dark.ShouldBeGreaterThan(0);
    string darkBlock = css[dark..];
    darkBlock.ShouldContain("--twe-primary-text: #ffd0a8;");
    darkBlock.ShouldContain("--twe-secondary-text: #9fe7ff;");
    darkBlock.ShouldContain("--twe-highlight-text: #b8f28d;");
    css[..dark].ShouldNotContain("#ffd0a8");

    // tokens.css derives links, accent text, and the focus ring from these three in dark mode.
    Dictionary<string, string> tokens = TokenBlock(ReadRepoFile(TokensCssPath), BrandStylesheet.DarkSelector);
    tokens["--twe-link"].ShouldBe("var(--twe-secondary-text)");
    tokens["--twe-accent-text"].ShouldBe("var(--twe-primary-text)");
    tokens["--twe-focus-ring"].ShouldBe("var(--twe-primary-text)");
    return Task.CompletedTask;
  }

  public static Task Round_Trip_Every_Property_Through_The_Brand_Document()
  {
    BrandOptions sample = SampleBrand();
    BrandOptions defaults = new();
    foreach (PropertyInfo property in typeof(BrandOptions).GetProperties().Where(property => property.CanWrite))
    {
      if (property.PropertyType == typeof(BrandTokenOptions))
      {
        foreach (PropertyInfo token in typeof(BrandTokenOptions).GetProperties())
        {
          token.GetValue(sample.Tokens).ShouldNotBe(token.GetValue(defaults.Tokens), "SampleBrand must set Tokens." + token.Name);
        }

        continue;
      }

      property.GetValue(sample).ShouldNotBe(property.GetValue(defaults), "SampleBrand must set " + property.Name);
    }

    string document = BrandDocument.Render(sample);
    document.ShouldStartWith("{\"" + BrandOptions.SectionName + "\":");

    ConfigurationBuilder builder = new();
    builder.AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(document)));
    BrandOptions bound = new();
    builder.Build().GetSection(BrandOptions.SectionName).Bind(bound);
    ShouldMatch(bound, sample);
    return Task.CompletedTask;
  }

  public static Task Render_The_Manifest_From_The_Brand()
  {
    BrandOptions brand = SampleBrand();
    using JsonDocument manifest = JsonDocument.Parse(WebManifest.Render(brand));
    JsonElement root = manifest.RootElement;
    root.GetProperty("name").GetString().ShouldBe("Northwind Traders");
    root.GetProperty("short_name").GetString().ShouldBe("Northwind Traders");
    root.GetProperty("theme_color").GetString().ShouldBe("#0b6e4f");
    root.GetProperty("background_color").GetString().ShouldBe("#fdfcf7");
    JsonElement icons = root.GetProperty("icons");
    icons.GetArrayLength().ShouldBe(2);
    icons[0].GetProperty("src").GetString().ShouldBe("images/brand/icon-512.png");
    icons[0].GetProperty("sizes").GetString().ShouldBe("512x512");
    icons[0].GetProperty("type").GetString().ShouldBe("image/png");
    icons[1].GetProperty("src").GetString().ShouldBe("images/brand/icon-192.svg");
    icons[1].GetProperty("type").GetString().ShouldBe("image/svg+xml");

    using JsonDocument fallback = JsonDocument.Parse(WebManifest.Render(new BrandOptions { SeedColor = "#abc" }));
    fallback.RootElement.GetProperty("theme_color").GetString().ShouldBe("#abc");
    fallback.RootElement.GetProperty("background_color").GetString().ShouldBe(BrandOptions.DefaultBackgroundColor);
    return Task.CompletedTask;
  }

  public static Task Render_A_Custom_Token_And_Logo_Height_And_Refuse_A_Non_Hex()
  {
    BrandOptions brand = new()
    {
      LogoHeight = 64,
      Tokens = new BrandTokenOptions { Primary = "#abcdef" }
    };

    string css = BrandStylesheet.Render(brand);
    css.ShouldContain("--twe-primary: #abcdef;");
    css.ShouldContain("--twe-logo-height: 64px;");
    css.ShouldContain("--twe-secondary: #0085b2;");
    css.ShouldNotContain("--twe-purple");

    Should.Throw<InvalidOperationException>(() => BrandStylesheet.Render(new BrandOptions
    {
      Tokens = new BrandTokenOptions { Primary = "nope" }
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
    // Reads the stored mode and the OS preference for the initial body attribute; never stores a mode.
    script.ShouldNotContain("existing.mode = ");
    script.ShouldContain("existing.mode === \"dark\" || existing.mode === \"light\"");
    script.ShouldContain("(prefers-color-scheme: dark)");
    script.ShouldContain("document.body.setAttribute(\"data-theme\", stored || (osDark ? \"dark\" : \"light\"));");
    script.ShouldNotContain("<");

    string harmonized = BrandThemeScript.Render(new BrandOptions());
    harmonized.ShouldContain("existing.color = \"#55409c\";");
    harmonized.ShouldContain("existing.exact = false;");

    Should.Throw<InvalidOperationException>(() => BrandThemeScript.Render(new BrandOptions { SeedColor = "red" }));
    return Task.CompletedTask;
  }

  public static Task Revalidate_The_Sheet_With_A_Strong_ETag()
  {
    BrandStylesheet.Response first = BrandStylesheet.Respond(new BrandOptions(), ifNoneMatch: null);
    first.StatusCode.ShouldBe(200);
    first.Body.ShouldNotBeNull();
    first.Body.ShouldContain("--twe-primary: #55409c;");
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

    BrandOptions custom = new() { Tokens = new BrandTokenOptions { Primary = "#abcdef" } };
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

  private static string ReadRepoFile(string relativePath) =>
    File.ReadAllText(Path.Combine(RepoRoot(), relativePath));

  /// <summary>Single-line <c>--name: value;</c> declarations of the first block that opens with <paramref name="selector"/>.</summary>
  private static Dictionary<string, string> TokenBlock(string css, string selector)
  {
    int open = css.IndexOf(selector + " {", StringComparison.Ordinal);
    open.ShouldBeGreaterThanOrEqualTo(0, selector);
    int close = css.IndexOf("\n}", open, StringComparison.Ordinal);
    Dictionary<string, string> declarations = new(StringComparer.Ordinal);
    foreach (string line in css[open..close].Split('\n'))
    {
      string text = line.Trim();
      if (!text.StartsWith("--", StringComparison.Ordinal) || text[^1] != ';')
      {
        continue;
      }

      int colon = text.IndexOf(':', StringComparison.Ordinal);
      if (colon > 0)
      {
        declarations[text[..colon]] = text[(colon + 1)..^1].Trim();
      }
    }

    return declarations;
  }

  /// <summary>The token's value in <paramref name="block"/>, following bare var() references (dark falls back to light).</summary>
  private static string Resolve(string name, Dictionary<string, string> block, Dictionary<string, string> light)
  {
    string? value = block.TryGetValue(name, out string? own) ? own : light.GetValueOrDefault(name);
    value.ShouldNotBeNull(name + " is not declared in tokens.css");
    bool bareReference = value.StartsWith("var(--", StringComparison.Ordinal)
      && value[^1] == ')'
      && !value.Contains(',', StringComparison.Ordinal);
    return bareReference ? Resolve(value[4..^1], block, light) : value;
  }

  private static BrandOptions SampleBrand() =>
    new()
    {
      ProductName = "Northwind Traders",
      LogoSrc = "images/brand/northwind-logo.svg",
      LogoAlt = "Northwind Traders logo",
      LogoWidth = 180,
      LogoHeight = 40,
      SeedColor = "#0b6e4f",
      ExactRamp = true,
      FaviconSrc = "images/brand/favicon.svg",
      AppleTouchIconSrc = "images/brand/apple-touch-icon.png",
      Icon192Src = "images/brand/icon-192.svg",
      Icon512Src = "images/brand/icon-512.png",
      OgImageSrc = "images/brand/og-image.png",
      ThemeColor = "#0b6e4f",
      BackgroundColor = "#fdfcf7",
      Tokens = new BrandTokenOptions
      {
        Primary = "#0b6e4f",
        PrimaryHover = "#11865f",
        PrimaryActive = "#06492f",
        PrimarySoft = "#a7e3c8",
        OnPrimary = "#fffffe",
        Secondary = "#c2571a",
        SecondaryHover = "#d86a2b",
        SecondaryActive = "#8a3a0d",
        SecondaryBright = "#ff9a52",
        Highlight = "#d4a017",
        HighlightSoft = "#f7e3a1",
        Info = "#1f6fb2",
        PrimaryDark = "#7fd8b0",
        SecondaryDark = "#ffb27d",
        HighlightDark = "#f2c94c"
      }
    };

  private static void ShouldMatch(BrandOptions actual, BrandOptions expected)
  {
    foreach (PropertyInfo property in typeof(BrandOptions).GetProperties().Where(property => property.CanWrite))
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
