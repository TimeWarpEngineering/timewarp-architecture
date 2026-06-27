#!/usr/bin/env -S dotnet --
#:project ../../../source/foundation/foundation-domain/foundation-domain.csproj
#:package TimeWarp.Jaribu
#:package Shouldly

// Jaribu duplicate of the Fixie suite in tests/foundation/foundation-domain-tests (task 046).
// Kept in parallel as a small worked example of BOTH test frameworks against the same class.
// Run standalone:  dotnet run tests/foundation/foundation-domain-jaribu-tests/enumeration.cs

#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif

namespace TimeWarp.Architecture.Enumerations.Jaribu.Tests
{

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Runtime.CompilerServices;
  using System.Threading.Tasks;
  using Shouldly;
  using TimeWarp.Architecture.Enumerations;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;

  /// <summary>A concrete <see cref="Enumeration"/> used to exercise the base class.</summary>
  internal sealed class Color : Enumeration
  {
    public static readonly Color Red = new(1, "Red", ["R", "FF0000"]);
    public static readonly Color Green = new(2, "Green", ["G"]);
    public static readonly Color Blue = new(3, "Blue", null);

    private Color(int value, string name, IReadOnlyList<string>? alternateCodes)
      : base(value, name, alternateCodes) { }
  }

  [TestTag("Enumeration")]
  public class Enumeration_GetAll_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<Enumeration_GetAll_Given_>();

    public static Task ConcreteEnumeration_Should_ReturnAllStaticFields()
    {
      Enumeration.GetAll<Color>().ToList().Count.ShouldBe(3);
      return Task.CompletedTask;
    }
  }

  [TestTag("Enumeration")]
  public class Enumeration_FromValue_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<Enumeration_FromValue_Given_>();

    public static Task ValidValue_Should_ReturnMatch()
    {
      Enumeration.FromValue<Color>(1).ShouldBe(Color.Red);
      return Task.CompletedTask;
    }

    public static Task InvalidValue_Should_ThrowInvalidOperationException()
    {
      Should.Throw<InvalidOperationException>(() => Enumeration.FromValue<Color>(99));
      return Task.CompletedTask;
    }
  }

  [TestTag("Enumeration")]
  public class Enumeration_FromName_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<Enumeration_FromName_Given_>();

    public static Task ValidName_Should_ReturnMatch()
    {
      Enumeration.FromName<Color>("Green").ShouldBe(Color.Green);
      return Task.CompletedTask;
    }

    public static Task InvalidName_Should_ThrowInvalidOperationException()
    {
      Should.Throw<InvalidOperationException>(() => Enumeration.FromName<Color>("Magenta"));
      return Task.CompletedTask;
    }
  }

  [TestTag("Enumeration")]
  public class Enumeration_FromAlternateCode_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<Enumeration_FromAlternateCode_Given_>();

    public static Task ValidCode_Should_ReturnMatch()
    {
      Enumeration.FromAlternateCode<Color>("FF0000").ShouldBe(Color.Red);
      return Task.CompletedTask;
    }

    public static Task InvalidCode_Should_ThrowInvalidOperationException()
    {
      Should.Throw<InvalidOperationException>(() => Enumeration.FromAlternateCode<Color>("ZZ"));
      return Task.CompletedTask;
    }
  }

  [TestTag("Enumeration")]
  public class Enumeration_FromString_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<Enumeration_FromString_Given_>();

    public static Task Name_Should_ReturnMatch()
    {
      Enumeration.FromString<Color>("Blue").ShouldBe(Color.Blue);
      return Task.CompletedTask;
    }

    public static Task AlternateCode_Should_ReturnMatch()
    {
      Enumeration.FromString<Color>("G").ShouldBe(Color.Green);
      return Task.CompletedTask;
    }

    public static Task InvalidInput_Should_ThrowInvalidOperationException()
    {
      Should.Throw<InvalidOperationException>(() => Enumeration.FromString<Color>("nope"));
      return Task.CompletedTask;
    }
  }

  [TestTag("Enumeration")]
  public class Enumeration_CompareTo_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<Enumeration_CompareTo_Given_>();

    public static Task TwoColors_Should_OrderByValue()
    {
      Color.Red.CompareTo(Color.Blue).ShouldBeLessThan(0);
      Color.Blue.CompareTo(Color.Red).ShouldBeGreaterThan(0);
      Color.Red.CompareTo(Color.Red).ShouldBe(0);
      return Task.CompletedTask;
    }

    public static Task Null_Should_SortFirst()
    {
      Color.Red.CompareTo(null).ShouldBeGreaterThan(0);
      return Task.CompletedTask;
    }

    public static Task NonEnumeration_Should_ThrowArgumentException()
    {
      Should.Throw<ArgumentException>(() => Color.Red.CompareTo("not an enumeration"));
      return Task.CompletedTask;
    }
  }

  [TestTag("Enumeration")]
  public class Enumeration_Equals_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<Enumeration_Equals_Given_>();

    public static Task SameValue_Should_BeEqual()
    {
      Color.Red.Equals(Color.Red).ShouldBeTrue();
      return Task.CompletedTask;
    }

    public static Task DifferentValue_Should_NotBeEqual()
    {
      Color.Red.Equals(Color.Blue).ShouldBeFalse();
      return Task.CompletedTask;
    }

    public static Task DifferentType_Should_NotBeEqual()
    {
      Color.Red.Equals("Red").ShouldBeFalse();
      return Task.CompletedTask;
    }

    public static Task Null_Should_NotBeEqual()
    {
      Color.Red.Equals(null).ShouldBeFalse();
      return Task.CompletedTask;
    }

    public static Task SameValue_Should_HaveSameHashCode()
    {
      Color.Red.GetHashCode().ShouldBe(Color.Red.GetHashCode());
      return Task.CompletedTask;
    }
  }

  [TestTag("Enumeration")]
  public class Enumeration_ToString_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<Enumeration_ToString_Given_>();

    public static Task AnyValue_Should_ReturnName()
    {
      Color.Green.ToString().ShouldBe("Green");
      return Task.CompletedTask;
    }
  }

  [TestTag("Enumeration")]
  public class Enumeration_Constructor_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<Enumeration_Constructor_Given_>();

    public static Task Values_Should_SetProperties()
    {
      Color.Red.Value.ShouldBe(1);
      Color.Red.Name.ShouldBe("Red");
      Color.Red.AlternateCodes.ShouldBe(new[] { "R", "FF0000" });
      return Task.CompletedTask;
    }

    public static Task NullAlternateCodes_Should_DefaultToEmpty()
    {
      Color.Blue.AlternateCodes.Count.ShouldBe(0);
      return Task.CompletedTask;
    }
  }

} // namespace TimeWarp.Architecture.Enumerations.Jaribu.Tests
