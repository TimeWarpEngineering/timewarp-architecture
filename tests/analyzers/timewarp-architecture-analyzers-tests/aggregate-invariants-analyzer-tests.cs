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

  private static CSharpAnalyzerTest<AggregateInvariantsAnalyzer, FixieVerifier> Test(string source) =>
    new()
    {
      TestState =
      {
        Sources =
        {
          ("FluentValidationStub.cs", FluentValidationStub),
          ("IAggregateRootStub.cs", AggregateRootStub),
          ("Aggregate.cs", source)
        }
      }
    };

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
}
