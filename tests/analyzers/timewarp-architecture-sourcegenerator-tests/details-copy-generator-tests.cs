#region Purpose
// Generated From copies details without sharing lists, and Clone keeps get-only values.
#endregion

#region Design
// The driver runs only DetailsCopyGenerator. Emit plus a collectible load proves UnsafeAccessor
// can write a get-only auto-property backing field: Avatar, Permissions, and Badge survive, and
// the copies are not the caller's instances. CopyBeyondDetails is the Version hook. An explicit
// ICloneable.Clone is left in place: a generated public Clone would not be the interface method.
#endregion

namespace TimeWarp.Architecture.SourceGenerator.Tests;

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using TimeWarp.Foundation.Contracts.Generators;

public class DetailsCopyGenerator_Tests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<DetailsCopyGenerator_Tests>();

  private const string Source =
    """
    namespace App
    {
      public interface IProfileDetails
      {
        string DisplayName { get; set; }
        System.Collections.Generic.List<string> Tags { get; set; }
        System.Collections.Generic.List<Widget> Badges { get; set; }
      }

      public sealed class Widget : System.ICloneable
      {
        public string Name { get; set; } = "";
        public object Clone() => new Widget { Name = Name };
      }

      public static partial class GetProfile
      {
        public sealed partial class Response : IProfileDetails
        {
          public string DisplayName { get; set; } = "";
          public string Avatar { get; }
          public long Version { get; }
          public System.Collections.Generic.List<string> Tags { get; set; } = new();
          public System.Collections.Generic.List<string> Permissions { get; }
          public Widget Badge { get; }
          public System.Collections.Generic.List<Widget> Badges { get; set; } = new();

          public Response(
            string displayName,
            string avatar,
            long version,
            System.Collections.Generic.List<string> tags,
            System.Collections.Generic.List<string> permissions,
            Widget badge,
            System.Collections.Generic.List<Widget> badges)
          {
            DisplayName = displayName;
            Avatar = avatar;
            Version = version;
            Tags = tags;
            Permissions = permissions;
            Badge = badge;
            Badges = badges;
          }
        }
      }

      public static partial class UpdateProfile
      {
        public sealed partial class Command : IProfileDetails
        {
          public string DisplayName { get; set; } = "";
          public System.Collections.Generic.List<string> Tags { get; set; } = new();
          public System.Collections.Generic.List<Widget> Badges { get; set; } = new();
          public long Version { get; set; }

          static partial void CopyBeyondDetails(Command command, IProfileDetails source)
          {
            if (source is GetProfile.Response loaded)
            {
              command.Version = loaded.Version;
            }
          }
        }
      }
    }
    """;

  public static async Task From_Copies_Values_And_Clone_Keeps_GetOnly_Members()
  {
    CSharpParseOptions parse = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
    CSharpCompilation compilation = CSharpCompilation.Create(
      "DetailsCopy.Execute",
      [CSharpSyntaxTree.ParseText(Source, parse)],
      References(),
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        .WithNullableContextOptions(NullableContextOptions.Enable));

    GeneratorDriver driver = CSharpGeneratorDriver.Create(
      [new DetailsCopyGenerator().AsSourceGenerator()],
      parseOptions: parse);
    driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation output, out ImmutableArray<Diagnostic> generatorDiagnostics);
    generatorDiagnostics.ShouldBeEmpty();

    string generated = "";
    foreach (GeneratedSourceResult source in driver.GetRunResult().Results[0].GeneratedSources)
    {
      generated += source.SourceText.ToString();
    }

    generated.ShouldContain("public static Command From(");
    generated.ShouldContain("static partial void CopyBeyondDetails(Command command,");
    generated.ShouldNotContain("public static partial void CopyBeyondDetails");
    generated.ShouldContain("MemberwiseClone()");
    generated.ShouldContain("UnsafeAccessor");
    generated.ShouldContain("<Permissions>k__BackingField");
    generated.ShouldContain("<Badge>k__BackingField");

    await using MemoryStream stream = new();
    Microsoft.CodeAnalysis.Emit.EmitResult emit = output.Emit(stream);
    emit.Success.ShouldBeTrue(string.Join('\n', emit.Diagnostics));
    stream.Position = 0;

    DetailsCopyLoadContext loadContext = new();
    Assembly assembly = loadContext.LoadFromStream(stream);
    Type responseType = assembly.GetType("App.GetProfile+Response")!;
    Type commandType = assembly.GetType("App.UpdateProfile+Command")!;
    Type widgetType = assembly.GetType("App.Widget")!;

    object badge = Activator.CreateInstance(widgetType)!;
    widgetType.GetProperty("Name")!.SetValue(badge, "gold");
    List<string> tags = ["a"];
    List<string> permissions = ["read"];
    object badges = Activator.CreateInstance(typeof(List<>).MakeGenericType(widgetType))!;
    badges.GetType().GetMethod("Add")!.Invoke(badges, [badge]);

    object response = Activator.CreateInstance(responseType, "Ada", "avatar", 7L, tags, permissions, badge, badges)!;
    object clone = ((ICloneable)response).Clone();

    responseType.GetProperty("Avatar")!.GetValue(clone).ShouldBe("avatar");
    responseType.GetProperty("Version")!.GetValue(clone).ShouldBe(7L);
    responseType.GetProperty("DisplayName")!.GetValue(clone).ShouldBe("Ada");
    responseType.GetProperty("DisplayName")!.SetValue(clone, "Bea");
    responseType.GetProperty("DisplayName")!.GetValue(response).ShouldBe("Ada");

    object cloneTags = responseType.GetProperty("Tags")!.GetValue(clone)!;
    cloneTags.ShouldNotBeSameAs(tags);
    ((List<string>)cloneTags).Add("b");
    tags.Count.ShouldBe(1);

    object clonePermissions = responseType.GetProperty("Permissions")!.GetValue(clone)!;
    clonePermissions.ShouldNotBeSameAs(permissions);
    ((List<string>)clonePermissions)[0].ShouldBe("read");

    object cloneBadge = responseType.GetProperty("Badge")!.GetValue(clone)!;
    cloneBadge.ShouldNotBeSameAs(badge);
    widgetType.GetProperty("Name")!.GetValue(cloneBadge).ShouldBe("gold");

    object cloneBadges = responseType.GetProperty("Badges")!.GetValue(clone)!;
    cloneBadges.ShouldNotBeSameAs(badges);
    object cloneBadgeElement = cloneBadges.GetType().GetProperty("Item")!.GetValue(cloneBadges, [0])!;
    cloneBadgeElement.ShouldNotBeSameAs(badge);
    widgetType.GetProperty("Name")!.GetValue(cloneBadgeElement).ShouldBe("gold");

    MethodInfo from = commandType.GetMethod("From")!;
    object command = from.Invoke(null, [response])!;
    commandType.GetProperty("DisplayName")!.GetValue(command).ShouldBe("Ada");
    commandType.GetProperty("Version")!.GetValue(command).ShouldBe(7L);
    commandType.GetProperty("Tags")!.GetValue(command).ShouldNotBeSameAs(tags);
    object commandBadges = commandType.GetProperty("Badges")!.GetValue(command)!;
    commandBadges.ShouldNotBeSameAs(badges);
    object commandBadge = commandBadges.GetType().GetProperty("Item")!.GetValue(commandBadges, [0])!;
    commandBadge.ShouldNotBeSameAs(badge);

    Should.Throw<TargetInvocationException>(() => from.Invoke(null, [null!]))
      .InnerException.ShouldBeOfType<ArgumentNullException>();
  }

  public static Task Explicit_Identity_Clone_Is_Not_Replaced()
  {
    const string source =
      """
      namespace App
      {
        public interface IProfileDetails { string Name { get; set; } }
        public static partial class GetWidget
        {
          public sealed partial class Response : IProfileDetails, System.ICloneable
          {
            public string Name { get; set; } = "";
            object System.ICloneable.Clone() => this;
          }
        }
      }
      """;

    CSharpParseOptions parse = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
    CSharpCompilation compilation = CSharpCompilation.Create(
      "DetailsCopy.ExplicitClone",
      [CSharpSyntaxTree.ParseText(source, parse)],
      References(),
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        .WithNullableContextOptions(NullableContextOptions.Enable));

    GeneratorDriver driver = CSharpGeneratorDriver.Create(
      [new DetailsCopyGenerator().AsSourceGenerator()],
      parseOptions: parse);
    driver = driver.RunGeneratorsAndUpdateCompilation(
      compilation,
      out Compilation _,
      out ImmutableArray<Diagnostic> generatorDiagnostics);
    generatorDiagnostics.ShouldBeEmpty();

    string generated = "";
    foreach (GeneratedSourceResult sourceResult in driver.GetRunResult().Results[0].GeneratedSources)
    {
      generated += sourceResult.SourceText.ToString();
    }

    generated.ShouldNotContain("object Clone()");
    return Task.CompletedTask;
  }

  private static MetadataReference[] References()
  {
    return ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
      .Split(Path.PathSeparator)
      .Where(static path => path.Length > 0)
      .Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path))
      .ToArray();
  }

  private sealed class DetailsCopyLoadContext : AssemblyLoadContext
  {
    public DetailsCopyLoadContext()
      : base(isCollectible: true)
    {
    }

    protected override Assembly? Load(AssemblyName assemblyName) => null;
  }
}
