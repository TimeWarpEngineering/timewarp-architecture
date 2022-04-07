namespace DelegateSourceGenerator_;

using FluentAssertions;
using TimeWarp.SourceCodeGenerators;
using static TimeWarp.SourceCodeGenerators.Tests.Infrastructure.SourceGeneratorTestHelper;

public class Should_
{

  public void Work()
  {
    string source = @"
namespace TimeWarp.SourceCodeGenerators.Tests.TestSource
{
  using TimeWarp.SourceCodeGenerators.Tests.TestSource.Echo;

  public partial class Composite: IEcho
  {
    [Delegate]
    private readonly IEcho MyEchoProperty;

    public Composite()
    {
      Echo = new DefaultEcho();
    }
  }
}
";

    string output = GetGeneratedOutput<DelegateSourceGenerator>(source);

    string expected =
@"#nullable enable
namespace TimeWarp.SourceCodeGenerators.Tests.TestSource
{
  public partial class Composite : TimeWarp.SourceCodeGenerators.Tests.TestSource.Echo.IEcho
  {
    public string Echo(string message, System.Collections.Generic.List<string> emotions) => MyEchoProperty.Echo(message, emotions);
    public string Method1(int aInt = 10) => MyEchoProperty.Method1(aInt);
    public string Method3(string aString, object? aObject = default) => MyEchoProperty.Method3(aString, aObject);
    public int MyProperty { get => MyEchoProperty.MyProperty; set => MyEchoProperty.MyProperty = value; }

    public int MyGetOnlyProperty { get => MyEchoProperty.MyGetOnlyProperty; }

    public int MySetOnlyProperty { set => MyEchoProperty.MySetOnlyProperty = value; }
  }
}";
    output.Should().Be(expected);
  }

  public void Work_Given_Single_Generic_Parameter()
  {
    string source = @"
namespace TimeWarp.SourceCodeGenerators.Tests.TestSource
{
  using MediatR;

  public partial class Composite<T>
  {
    [Delegate]
    private readonly ISender TestSender;

    public Composite()
    {
      ScopedSender = new TestSender();
    }
  }
}
";

    string output = GetGeneratedOutput<DelegateSourceGenerator>(source);

    string expected =
@"#nullable enable
namespace TimeWarp.SourceCodeGenerators.Tests.TestSource
{
  public partial class Composite<T> : MediatR.ISender
  {
    public System.Threading.Tasks.Task<TResponse> Send<TResponse>(MediatR.IRequest<TResponse> request, System.Threading.CancellationToken cancellationToken = default) => TestSender.Send(request, cancellationToken);
    public System.Threading.Tasks.Task<object?> Send(object request, System.Threading.CancellationToken cancellationToken = default) => TestSender.Send(request, cancellationToken);
    public System.Collections.Generic.IAsyncEnumerable<TResponse> CreateStream<TResponse>(MediatR.IStreamRequest<TResponse> request, System.Threading.CancellationToken cancellationToken = default) => TestSender.CreateStream(request, cancellationToken);
    public System.Collections.Generic.IAsyncEnumerable<object?> CreateStream(object request, System.Threading.CancellationToken cancellationToken = default) => TestSender.CreateStream(request, cancellationToken);
  }
}";
    output.Should().Be(expected);
  }

  public void Work_Given_Two_Generic_Parameters()
  {
    string source = @"
namespace TimeWarp.SourceCodeGenerators.Tests.TestSource
{
  using MediatR;

  public partial class Composite<TPerson,TAnimal>
  {
    [Delegate]
    private readonly ISender TestSender;

    public Composite()
    {
      ScopedSender = new TestSender();
    }
  }
}
";

    string output = GetGeneratedOutput<DelegateSourceGenerator>(source);

    string expected =
@"#nullable enable
namespace TimeWarp.SourceCodeGenerators.Tests.TestSource
{
  public partial class Composite<TPerson, TAnimal> : MediatR.ISender
  {
    public System.Threading.Tasks.Task<TResponse> Send<TResponse>(MediatR.IRequest<TResponse> request, System.Threading.CancellationToken cancellationToken = default) => TestSender.Send(request, cancellationToken);
    public System.Threading.Tasks.Task<object?> Send(object request, System.Threading.CancellationToken cancellationToken = default) => TestSender.Send(request, cancellationToken);
    public System.Collections.Generic.IAsyncEnumerable<TResponse> CreateStream<TResponse>(MediatR.IStreamRequest<TResponse> request, System.Threading.CancellationToken cancellationToken = default) => TestSender.CreateStream(request, cancellationToken);
    public System.Collections.Generic.IAsyncEnumerable<object?> CreateStream(object request, System.Threading.CancellationToken cancellationToken = default) => TestSender.CreateStream(request, cancellationToken);
  }
}";

    output.Should().Be(expected);
  }
}
