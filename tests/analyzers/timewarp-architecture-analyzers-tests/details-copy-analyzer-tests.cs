#region Purpose
// TWA0047–TWA0049: a mutable details response must not clone as itself, and From or Clone need a partial type.
#endregion

namespace DetailsCopyAnalyzer_;

using Microsoft.CodeAnalysis.CSharp.Testing;
using TimeWarp.Architecture.Analyzers;
using TimeWarp.Architecture.Analyzers.Tests;

public class DetailsCopyAnalyzer_Tests
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<DetailsCopyAnalyzer_Tests>();

  private static CSharpAnalyzerTest<DetailsCopyAnalyzer, RoslynTestVerifier> Test(string source) =>
    new()
    {
      TestState = { Sources = { source } }
    };

  public static async Task Identity_Clone_On_Mutable_Details_Response_Flags_TWA0047()
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
            public object {|TWA0047:Clone|}() => this;
          }
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Explicit_Identity_Clone_Flags_TWA0047()
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
            object System.ICloneable.{|TWA0047:Clone|}() => this;
          }
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Cast_Identity_Clone_Flags_TWA0047()
  {
    const string source =
      """
      namespace App
      {
        public interface IProfileDetails { string Name { get; set; } }
        public static partial class GetWidget
        {
          public sealed partial class Response : IProfileDetails
          {
            public string Name { get; set; } = "";
            public object {|TWA0047:Clone|}() { return (object)this; }
          }
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Memberwise_Clone_Is_Clean()
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
            public object Clone() => MemberwiseClone();
          }
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Immutable_Identity_Clone_Without_Details_Is_Clean()
  {
    const string source =
      """
      namespace App
      {
        public sealed class Snapshot : System.ICloneable
        {
          public string Name { get; } = "";
          public object Clone() => this;
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task GetOnly_Details_Identity_Clone_Is_Clean()
  {
    const string source =
      """
      namespace App
      {
        public interface IAuditDetails { string Name { get; } }
        public sealed class Response : IAuditDetails, System.ICloneable
        {
          public string Name { get; } = "";
          public object Clone() => this;
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task NonPartial_Command_Flags_TWA0048()
  {
    const string source =
      """
      namespace App
      {
        public interface IProfileDetails { string Name { get; set; } }
        public static partial class UpdateWidget
        {
          public sealed class {|TWA0048:Command|} : IProfileDetails
          {
            public string Name { get; set; } = "";
          }
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task NonPartial_Response_Flags_TWA0048()
  {
    const string source =
      """
      namespace App
      {
        public interface IProfileDetails { string Name { get; set; } }
        public static partial class GetWidget
        {
          public sealed class {|TWA0048:Response|} : IProfileDetails
          {
            public string Name { get; set; } = "";
          }
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task NonPartial_Parent_Flags_TWA0048()
  {
    const string source =
      """
      namespace App
      {
        public interface IProfileDetails { string Name { get; set; } }
        public static class {|TWA0048:GetWidget|}
        {
          public sealed partial class Response : IProfileDetails
          {
            public string Name { get; set; } = "";
          }
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Partial_Command_Without_Parameterless_Constructor_Flags_TWA0049()
  {
    const string source =
      """
      namespace App
      {
        public interface IProfileDetails { string Name { get; set; } }
        public static partial class UpdateWidget
        {
          public sealed partial class {|TWA0049:Command|} : IProfileDetails
          {
            public string Name { get; set; } = "";
            public Command(string name) { Name = name; }
          }
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Partial_Command_With_Implicit_Constructor_Is_Clean()
  {
    const string source =
      """
      namespace App
      {
        public interface IProfileDetails { string Name { get; set; } }
        public static partial class UpdateWidget
        {
          public sealed partial class Command : IProfileDetails
          {
            public string Name { get; set; } = "";
          }
        }
      }
      """;

    await Test(source).RunAsync();
  }
}
