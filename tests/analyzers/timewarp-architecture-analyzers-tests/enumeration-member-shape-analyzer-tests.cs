#region Purpose
// Tests for TWA0028: Enumeration subclass members must be public static readonly fields.
#endregion

#region Design
// A same-named Enumeration stub in TimeWarp.Foundation.Enumerations stands in for foundation-domain,
// since the analyzer matches the base class by name + namespace rather than an assembly reference.
#endregion

// ReSharper disable InconsistentNaming
namespace EnumerationMemberShapeAnalyzer_;

using Microsoft.CodeAnalysis.CSharp.Testing;
using TimeWarp.Architecture.Analyzers;
using TimeWarp.Architecture.Analyzers.Tests;

public class Should_Enforce_Enumeration_Member_Shape
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should_Enforce_Enumeration_Member_Shape>();

  private const string EnumerationStub =
    """
    namespace TimeWarp.Foundation.Enumerations
    {
      public abstract class Enumeration
      {
        protected Enumeration(int value, string name) { }
      }
    }
    """;

  private static CSharpAnalyzerTest<EnumerationMemberShapeAnalyzer, RoslynTestVerifier> Test(string source)
  {
    CSharpAnalyzerTest<EnumerationMemberShapeAnalyzer, RoslynTestVerifier> test = new();
    test.TestState.Sources.Add(("EnumerationStub.cs", EnumerationStub));
    test.TestState.Sources.Add(("Color.cs", source));
    return test;
  }

  public static async Task Given_Public_Static_Readonly_Fields_IsClean()
  {
    const string source =
      """
      using TimeWarp.Foundation.Enumerations;

      namespace Domain
      {
        public class Color : Enumeration
        {
          public static readonly Color Red = new(1, "Red");
          public static readonly Color Blue = new BlueColor();
          private static readonly int Count = 2;
          private Color(int value, string name) : base(value, name) { }

          private sealed class BlueColor : Color
          {
            public BlueColor() : base(2, "Blue") { }
          }
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_Static_Property_Member_Flags()
  {
    const string source =
      """
      using TimeWarp.Foundation.Enumerations;

      namespace Domain
      {
        public sealed class Color : Enumeration
        {
          public static Color {|TWA0028:Red|} { get; } = new(1, "Red");
          private Color(int value, string name) : base(value, name) { }
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_NonPublic_Field_Member_Flags()
  {
    const string source =
      """
      using TimeWarp.Foundation.Enumerations;

      namespace Domain
      {
        public sealed class Color : Enumeration
        {
          internal static readonly Color {|TWA0028:Red|} = new(1, "Red");
          private Color(int value, string name) : base(value, name) { }
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_Mutable_Public_Field_Member_Flags()
  {
    const string source =
      """
      using TimeWarp.Foundation.Enumerations;

      namespace Domain
      {
        public sealed class Color : Enumeration
        {
          public static Color {|TWA0028:Red|} = new(1, "Red");
          private Color(int value, string name) : base(value, name) { }
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_Indirect_Subclass_Is_Checked()
  {
    const string source =
      """
      using TimeWarp.Foundation.Enumerations;

      namespace Domain
      {
        public abstract class Policy : Enumeration
        {
          protected Policy(int value, string name) : base(value, name) { }
        }

        public sealed class CorsPolicy : Policy
        {
          private static readonly CorsPolicy {|TWA0028:Any|} = new(0, "Any");
          private CorsPolicy(int value, string name) : base(value, name) { }
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_NonEnumeration_Type_IsClean()
  {
    const string source =
      """
      namespace Domain
      {
        public sealed class Settings
        {
          public static Settings Default { get; } = new();
          private static Settings cached = new();
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_Same_Named_Base_In_Other_Namespace_IsClean()
  {
    const string source =
      """
      namespace Other
      {
        public abstract class Enumeration { }

        public sealed class Color : Enumeration
        {
          public static Color Red { get; } = new();
        }
      }
      """;

    await Test(source).RunAsync();
  }
}
