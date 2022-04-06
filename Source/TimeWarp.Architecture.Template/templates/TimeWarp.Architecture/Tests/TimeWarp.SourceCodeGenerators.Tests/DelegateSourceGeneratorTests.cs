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
    private readonly IEcho Echo;

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
    string TimeWarp.SourceCodeGenerators.Tests.TestSource.Echo.IEcho.Echo(string message, System.Collections.Generic.List<string> emotions) => Echo.Echo(message, emotions);
    public int MyProperty { get => Echo.MyProperty; set => Echo.MyProperty = value; }

    public int MyGetOnlyProperty { get => Echo.MyGetOnlyProperty; }

    public int MySetOnlyProperty { set => Echo.MySetOnlyProperty = value; }
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
    System.Threading.Tasks.Task<TResponse> MediatR.ISender.Send<TResponse>(MediatR.IRequest<TResponse> request, System.Threading.CancellationToken cancellationToken) => TestSender.Send(request, cancellationToken);
    System.Threading.Tasks.Task<object?> MediatR.ISender.Send(object request, System.Threading.CancellationToken cancellationToken) => TestSender.Send(request, cancellationToken);
    System.Collections.Generic.IAsyncEnumerable<TResponse> MediatR.ISender.CreateStream<TResponse>(MediatR.IStreamRequest<TResponse> request, System.Threading.CancellationToken cancellationToken) => TestSender.CreateStream(request, cancellationToken);
    System.Collections.Generic.IAsyncEnumerable<object?> MediatR.ISender.CreateStream(object request, System.Threading.CancellationToken cancellationToken) => TestSender.CreateStream(request, cancellationToken);
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
    System.Threading.Tasks.Task<TResponse> MediatR.ISender.Send<TResponse>(MediatR.IRequest<TResponse> request, System.Threading.CancellationToken cancellationToken) => TestSender.Send(request, cancellationToken);
    System.Threading.Tasks.Task<object?> MediatR.ISender.Send(object request, System.Threading.CancellationToken cancellationToken) => TestSender.Send(request, cancellationToken);
    System.Collections.Generic.IAsyncEnumerable<TResponse> MediatR.ISender.CreateStream<TResponse>(MediatR.IStreamRequest<TResponse> request, System.Threading.CancellationToken cancellationToken) => TestSender.CreateStream(request, cancellationToken);
    System.Collections.Generic.IAsyncEnumerable<object?> MediatR.ISender.CreateStream(object request, System.Threading.CancellationToken cancellationToken) => TestSender.CreateStream(request, cancellationToken);
  }
}";
    output.Should().Be(expected);
  }
}
