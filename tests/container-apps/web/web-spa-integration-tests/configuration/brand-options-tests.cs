#region Purpose
// Pins BrandOptions binding, the validator, /brand.css, and the Fluent startup script.
#endregion

#region Design
// No host. The stylesheet and the script are pure functions of BrandOptions. The validator is the
// same internal type ValidateOnStart runs. tokens.css is read from the repo so a default hex
// cannot drift from the fallback sheet.
#endregion

namespace BrandOptions_;

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
    validator.Validate(new BrandOptions { LogoSrc = "/images/mark.svg" }).IsValid.ShouldBeTrue();
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
    css.ShouldContain("--twe-purple: #55409c;");
    css.ShouldContain("--twe-primary: #55409c;");
    css.ShouldContain("--twe-blue: #0085b2;");
    css.ShouldContain("--twe-green: #69a144;");
    css.ShouldContain("--twe-info: #0085b2;");
    css.ShouldContain("--twe-on-primary: #ffffff;");
    css.ShouldContain("--twe-logo-height: 48px;");
    css.ShouldNotContain("<");

    string tokens = File.ReadAllText(Path.Combine(RepoRoot(), "source/container-apps/web/projects/web-spa/wwwroot/css/tokens.css"));
    tokens.ShouldContain("--twe-purple: #55409c;");
    tokens.ShouldContain("--twe-logo-height: 48px;");
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
}
