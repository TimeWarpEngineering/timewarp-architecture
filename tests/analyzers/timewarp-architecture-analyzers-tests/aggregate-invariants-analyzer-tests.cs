#region Purpose
// Tests for TWA0011/TWA0012: aggregate roots must declare a nested Invariants validator, and it
// must be private.
#endregion

#region Design
// Minimal stubs (no real package references) mirror ContractNullabilityValidatorAnalyzer's approach:
// a FluentValidation shape stub plus a same-named IAggregateRoot stub, since the analyzer matches
// both by simple name/base-chain shape rather than a hard assembly reference.
#endregion

// ReSharper disable InconsistentNaming
namespace AggregateInvariantsAnalyzer_;

using Microsoft.CodeAnalysis.CSharp.Testing;
using TimeWarp.Architecture.Analyzers;
using TimeWarp.Architecture.Analyzers.Tests;

public class Should_Enforce_Nested_Invariants
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should_Enforce_Nested_Invariants>();

  private const string FluentValidationStub =
    """
    #nullable enable
    using System;
    using System.Linq.Expressions;

    namespace FluentValidation
    {
      public interface IRuleBuilderOptions<T, TProperty> { }
      public interface IRuleBuilderInitial<T, TProperty> : IRuleBuilderOptions<T, TProperty> { }

      public abstract class AbstractValidator<T>
      {
        protected IRuleBuilderInitial<T, TProperty> RuleFor<TProperty>(Expression<Func<T, TProperty>> expression) => null!;
      }

      public static class DefaultValidatorExtensions
      {
        public static IRuleBuilderOptions<T, TProperty> NotEmpty<T, TProperty>(this IRuleBuilderOptions<T, TProperty> rule) => rule;
      }
    }
    """;

  private const string AggregateRootStub =
    """
    namespace TimeWarp.Foundation.Entities
    {
      public interface IAggregateRoot { }
    }
    """;

  private static CSharpAnalyzerTest<AggregateInvariantsAnalyzer, RoslynTestVerifier> Test(string source) =>
    Test(("Aggregate.cs", source));

  private static CSharpAnalyzerTest<AggregateInvariantsAnalyzer, RoslynTestVerifier> Test(params (string path, string source)[] files)
  {
    CSharpAnalyzerTest<AggregateInvariantsAnalyzer, RoslynTestVerifier> test = new();
    test.TestState.Sources.Add(("FluentValidationStub.cs", FluentValidationStub));
    test.TestState.Sources.Add(("IAggregateRootStub.cs", AggregateRootStub));
    foreach ((string path, string source) in files)
    {
      test.TestState.Sources.Add((path, source));
    }

    return test;
  }

  public static async Task Given_AggregateRoot_With_Private_Invariants_IsClean()
  {
    const string Source =
      """
      using FluentValidation;
      using TimeWarp.Foundation.Entities;

      namespace Domain
      {
        public sealed class Widget : IAggregateRoot
        {
          public string Name { get; set; } = "";

          private sealed class Invariants : AbstractValidator<Widget>
          {
            public Invariants() => RuleFor(w => w.Name).NotEmpty();
          }
        }
      }
      """;

    await Test(Source).RunAsync();
  }

  public static async Task Given_AggregateRoot_Without_Invariants_Flags()
  {
    const string Source =
      """
      using TimeWarp.Foundation.Entities;

      namespace Domain
      {
        public sealed class {|TWA0011:Widget|} : IAggregateRoot
        {
          public string Name { get; set; } = "";
        }
      }
      """;

    await Test(Source).RunAsync();
  }

  public static async Task Given_NonAggregateRoot_IsClean()
  {
    const string Source =
      """
      namespace Domain
      {
        public sealed class PlainType
        {
          public string Name { get; set; } = "";
        }
      }
      """;

    await Test(Source).RunAsync();
  }

  public static async Task Given_AbstractAggregateRoot_IsClean()
  {
    const string Source =
      """
      using TimeWarp.Foundation.Entities;

      namespace Domain
      {
        public abstract class AbstractWidget : IAggregateRoot
        {
          public string Name { get; set; } = "";
        }
      }
      """;

    await Test(Source).RunAsync();
  }

  public static async Task Given_AggregateRoot_With_Public_Invariants_Flags()
  {
    const string Source =
      """
      using FluentValidation;
      using TimeWarp.Foundation.Entities;

      namespace Domain
      {
        public sealed class Widget : IAggregateRoot
        {
          public string Name { get; set; } = "";

          public sealed class {|TWA0012:Invariants|} : AbstractValidator<Widget>
          {
            public Invariants() => RuleFor(w => w.Name).NotEmpty();
          }
        }
      }
      """;

    await Test(Source).RunAsync();
  }

  // --- M2: shape drift between analyzer and runtime guard --------------------------------------

  public static async Task Given_Abstract_Nested_Validator_StillFlags_Missing()
  {
    // Abstract nested validator satisfies the base-chain check but can never be instantiated by
    // DomainInvariantsGuard — must not satisfy TWA0011.
    const string Source =
      """
      using FluentValidation;
      using TimeWarp.Foundation.Entities;

      namespace Domain
      {
        public sealed class {|TWA0011:Widget|} : IAggregateRoot
        {
          public string Name { get; set; } = "";

          private abstract class Invariants : AbstractValidator<Widget>
          {
          }
        }
      }
      """;

    await Test(Source).RunAsync();
  }

  public static async Task Given_CtorParameterized_Nested_Validator_StillFlags_Missing()
  {
    // Only constructor takes a parameter — DomainInvariantsGuard's Activator.CreateInstance(...,
    // nonPublic: true) has no parameterless overload to call, so this can never be instantiated at
    // save time — must not satisfy TWA0011.
    const string Source =
      """
      using FluentValidation;
      using TimeWarp.Foundation.Entities;

      namespace Domain
      {
        public sealed class {|TWA0011:Widget|} : IAggregateRoot
        {
          public string Name { get; set; } = "";

          private sealed class Invariants : AbstractValidator<Widget>
          {
            public Invariants(string label)
            {
            }
          }
        }
      }
      """;

    await Test(Source).RunAsync();
  }

  public static async Task Given_WrongNamespace_AbstractValidator_StillFlags_Missing()
  {
    // Simple-name match on "AbstractValidator" but NOT FluentValidation's — must not satisfy
    // TWA0011 (the guard would never find this via IValidator<T> either).
    const string Source =
      """
      using TimeWarp.Foundation.Entities;

      namespace Domain
      {
        public abstract class AbstractValidator<T> { }

        public sealed class {|TWA0011:Widget|} : IAggregateRoot
        {
          public string Name { get; set; } = "";

          private sealed class Invariants : AbstractValidator<Widget>
          {
          }
        }
      }
      """;

    await Test(Source).RunAsync();
  }

  // --- M3: every non-private qualifying validator is flagged, not just the first ----------------

  public static async Task Given_Two_Public_Nested_Validators_Flags_Both()
  {
    const string Source =
      """
      using FluentValidation;
      using TimeWarp.Foundation.Entities;

      namespace Domain
      {
        public sealed class Widget : IAggregateRoot
        {
          public string Name { get; set; } = "";

          public sealed class {|TWA0012:InvariantsA|} : AbstractValidator<Widget>
          {
            public InvariantsA() => RuleFor(w => w.Name).NotEmpty();
          }

          public sealed class {|TWA0012:InvariantsB|} : AbstractValidator<Widget>
          {
            public InvariantsB() => RuleFor(w => w.Name).NotEmpty();
          }
        }
      }
      """;

    await Test(Source).RunAsync();
  }

  // --- M10: indirect IAggregateRoot, name-vs-shape, internal accessibility ----------------------

  public static async Task Given_IAggregateRoot_Implemented_Via_BaseClass_Flags_Missing()
  {
    const string basePath = "aggregate-base.cs";
    const string BaseSource =
      """
      using TimeWarp.Foundation.Entities;

      namespace Domain
      {
        public abstract class AggregateBase : IAggregateRoot
        {
        }
      }
      """;

    const string derivedPath = "widget.cs";
    const string DerivedSource =
      """
      namespace Domain
      {
        public sealed class {|TWA0011:Widget|} : AggregateBase
        {
          public string Name { get; set; } = "";
        }
      }
      """;

    await Test((basePath, BaseSource), (derivedPath, DerivedSource)).RunAsync();
  }

  public static async Task Given_IAggregateRoot_Implemented_Via_Extended_Interface_Flags_Missing()
  {
    const string interfacePath = "i-widget-root.cs";
    const string InterfaceSource =
      """
      using TimeWarp.Foundation.Entities;

      namespace Domain
      {
        public interface IWidgetRoot : IAggregateRoot
        {
        }
      }
      """;

    const string typePath = "widget.cs";
    const string TypeSource =
      """
      namespace Domain
      {
        public sealed class {|TWA0011:Widget|} : IWidgetRoot
        {
          public string Name { get; set; } = "";
        }
      }
      """;

    await Test((interfacePath, InterfaceSource), (typePath, TypeSource)).RunAsync();
  }

  public static async Task Given_SameNamed_NonValidator_Nested_Type_StillFlags_Missing()
  {
    // A nested type literally named "Invariants" that does NOT derive AbstractValidator<Widget> —
    // pins the Design region's "matching by base-chain shape, not by name" claim.
    const string Source =
      """
      using TimeWarp.Foundation.Entities;

      namespace Domain
      {
        public sealed class {|TWA0011:Widget|} : IAggregateRoot
        {
          public string Name { get; set; } = "";

          private sealed class Invariants
          {
          }
        }
      }
      """;

    await Test(Source).RunAsync();
  }

  public static async Task Given_Internal_Nested_Validator_Flags_TWA0012()
  {
    const string Source =
      """
      using FluentValidation;
      using TimeWarp.Foundation.Entities;

      namespace Domain
      {
        public sealed class Widget : IAggregateRoot
        {
          public string Name { get; set; } = "";

          internal sealed class {|TWA0012:Invariants|} : AbstractValidator<Widget>
          {
            public Invariants() => RuleFor(w => w.Name).NotEmpty();
          }
        }
      }
      """;

    await Test(Source).RunAsync();
  }
}
