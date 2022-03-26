namespace TimeWarp.SourceCodeGenerators.Tests.TestSource.Echo
{
  using System;
  using System.Collections.Generic;

  public interface IEcho
  {
    public int MyProperty { get; set; }

    public int MyGetOnlyProperty { get;}

    public int MySetOnlyProperty { set; }

    //public IEcho MyEchoProperty { get; set; }

    //event EventHandler ShapeChanged;

    string Echo(string message, List<string> emotions);
    //string Hello(string name, List<string> emotions);
    //string Method3(string name, List<string> emotions);

  }
}
