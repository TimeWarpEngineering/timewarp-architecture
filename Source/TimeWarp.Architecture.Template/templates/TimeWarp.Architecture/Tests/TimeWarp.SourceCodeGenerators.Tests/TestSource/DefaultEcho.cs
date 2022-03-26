namespace TimeWarp.SourceCodeGenerators.Tests.TestSource
{
  using System;
  using System.Collections.Generic;
  using TimeWarp.Architecture.Testing;
  using TimeWarp.SourceCodeGenerators.Tests.TestSource.Echo;

  [NotTest]
  internal class DefaultEcho : IEcho
  {
    public string this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public int MyProperty { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public int MyGetOnlyProperty => throw new NotImplementedException();

    public int MySetOnlyProperty { set => throw new NotImplementedException(); }
    public IEcho MyEchoProperty { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    //public event EventHandler ShapeChanged;

    public string Hello(string name, List<string> emotions) => $"Hello {name}";
    public string Echo(string message, List<string> emotions) => throw new NotImplementedException();
    public string Method3(string name, List<string> emotions) => throw new NotImplementedException();
  }
}
