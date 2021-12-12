namespace TimeWarp.Blazor.InterfaceDelegate
{
  public partial class DelegateSourceGenerator
  {
    public class MethodGeneratorModel
    {
      public string ReturnType { get; set; }
      public string InterfaceName { get; set; }
      public string MethodName { get; set; }
      public string GenericType { get; set; }
      public string FieldName { get; set; }
      public string Parameters { get; set; }
      public string Arguments { get; set; }
    }
  }
}
