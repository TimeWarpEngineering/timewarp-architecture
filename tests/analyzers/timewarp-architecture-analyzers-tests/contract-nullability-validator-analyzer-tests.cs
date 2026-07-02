// ReSharper disable InconsistentNaming
namespace ContractNullabilityValidatorAnalyzer_;

using Microsoft.CodeAnalysis.CSharp.Testing;
using TimeWarp.Architecture.Analyzers;
using TimeWarp.Architecture.Analyzers.Tests;

public class Should_Flag_Contradictions
{
  // Minimal FluentValidation surface so the test compilation resolves AbstractValidator/RuleFor/
  // NotEmpty/NotNull without referencing the real package. The analyzer matches these by shape/name.
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
        public static IRuleBuilderOptions<T, TProperty> NotNull<T, TProperty>(this IRuleBuilderOptions<T, TProperty> rule) => rule;
        public static IRuleBuilderOptions<T, TProperty> SetValidator<T, TProperty>(this IRuleBuilderOptions<T, TProperty> rule, AbstractValidator<TProperty> validator) => rule;
        public static IRuleBuilderOptions<T, TProperty> WithMessage<T, TProperty>(this IRuleBuilderOptions<T, TProperty> rule, string message) => rule;
      }
    }
    """;

  private static CSharpAnalyzerTest<ContractNullabilityValidatorAnalyzer, FixieVerifier> Test(string source) =>
    new()
    {
      TestState =
      {
        Sources =
        {
          ("FluentValidationStub.cs", FluentValidationStub),
          ("Contract.cs", source)
        }
      }
    };

  // --- TWPA0002: nullable + presence rule --------------------------------------------------------

  public static async Task Given_NullableProperty_With_NotEmpty()
  {
    const string Source =
      """
      #nullable enable
      using FluentValidation;

      namespace Contracts
      {
        public class Query
        {
          public string? {|TWPA0002:Name|} { get; set; }
        }

        public class QueryValidator : AbstractValidator<Query>
        {
          public QueryValidator() => RuleFor(command => command.Name).NotEmpty();
        }
      }
      """;

    await Test(Source).RunAsync();
  }

  public static async Task Given_NullableProperty_With_NotNull()
  {
    const string Source =
      """
      #nullable enable
      using FluentValidation;

      namespace Contracts
      {
        public class Command
        {
          public string? {|TWPA0002:EventName|} { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
          public CommandValidator() => RuleFor(x => x.EventName).NotNull();
        }
      }
      """;

    await Test(Source).RunAsync();
  }

  // --- TWPA0003: = string.Empty / = "" + presence rule -------------------------------------------

  public static async Task Given_EmptyStringDefault_With_NotEmpty()
  {
    const string Source =
      """
      #nullable enable
      using FluentValidation;

      namespace Contracts
      {
        public class Command
        {
          public string {|TWPA0003:Title|} { get; set; } = string.Empty;
        }

        public class CommandValidator : AbstractValidator<Command>
        {
          public CommandValidator() => RuleFor(x => x.Title).NotEmpty();
        }
      }
      """;

    await Test(Source).RunAsync();
  }

  public static async Task Given_EmptyStringLiteralDefault_With_NotEmpty()
  {
    const string Source =
      """
      #nullable enable
      using FluentValidation;

      namespace Contracts
      {
        public class Command
        {
          public string {|TWPA0003:Title|} { get; set; } = "";
        }

        public class CommandValidator : AbstractValidator<Command>
        {
          public CommandValidator() => RuleFor(x => x.Title).NotEmpty();
        }
      }
      """;

    await Test(Source).RunAsync();
  }

  // --- Negatives (must be clean) -----------------------------------------------------------------

  public static async Task Given_NullableProperty_Without_Rule_IsClean()
  {
    // string? with no presence rule is a legitimate optional field.
    const string Source =
      """
      #nullable enable
      using FluentValidation;

      namespace Contracts
      {
        public class Command
        {
          public string? Nickname { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
          public CommandValidator() { }
        }
      }
      """;

    await Test(Source).RunAsync();
  }

  public static async Task Given_NonNullable_NullBang_With_NotEmpty_IsClean()
  {
    // The compliant shape: non-nullable + = null! + NotEmpty().
    const string Source =
      """
      #nullable enable
      using FluentValidation;

      namespace Contracts
      {
        public class Command
        {
          public string Name { get; set; } = null!;
        }

        public class CommandValidator : AbstractValidator<Command>
        {
          public CommandValidator() => RuleFor(x => x.Name).NotEmpty();
        }
      }
      """;

    await Test(Source).RunAsync();
  }

  public static async Task Given_WholeObjectRuleFor_IsClean()
  {
    // RuleFor(x => x) targets no specific property (the whole-object / SetValidator shape) — must
    // not flag, even with a nullable property present and a presence rule on the object.
    const string Source =
      """
      #nullable enable
      using FluentValidation;

      namespace Contracts
      {
        public class Command
        {
          public string? Name { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
          public CommandValidator() => RuleFor(x => x).NotNull();
        }
      }
      """;

    await Test(Source).RunAsync();
  }

  public static async Task Given_NonTrivialLambdaBody_IsClean()
  {
    // x => x.Name.Trim() is not a plain member access — conservatively skipped.
    const string Source =
      """
      #nullable enable
      using FluentValidation;

      namespace Contracts
      {
        public class Command
        {
          public string? Name { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
          public CommandValidator() => RuleFor(x => x.Name!.Trim()).NotEmpty();
        }
      }
      """;

    await Test(Source).RunAsync();
  }
}
