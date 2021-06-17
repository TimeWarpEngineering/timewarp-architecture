namespace Console_CSharp
{
  using System.CommandLine;
  using System.CommandLine.Builder;
  using System.CommandLine.Parsing;
  using System.Threading.Tasks;

  internal class Program
  {
    internal static Task<int> Main(string[] aArgumentArray)
    {
      Parser parser = new TimeWarpCommandLineBuilder().Build();

      return parser.InvokeAsync(aArgumentArray);
    }
  }
}