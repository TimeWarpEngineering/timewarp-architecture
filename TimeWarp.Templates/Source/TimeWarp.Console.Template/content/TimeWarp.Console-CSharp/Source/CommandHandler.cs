namespace Console_CSharp;

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Reflection;
using System.Threading.Tasks;
using MediatR;

internal class MediatorCommandHandler : ICommandHandler
{
  private IMediator Mediator { get; }

  private Type Type { get; }

  public MediatorCommandHandler(Type aType, IMediator aMediator)
  {
    Type = aType;
    Mediator = aMediator;
  }

  public async Task<int> InvokeAsync(InvocationContext aInvocationContext)
  {
    try
    {
      var request = (IRequest)Activator.CreateInstance(Type);
      foreach (SymbolResult symbolResult in aInvocationContext.ParseResult.CommandResult.Children)
      {
        Type optionResultType = typeof(OptionResult);

        object theArgumentConversionResult =
          optionResultType.GetProperty("ArgumentConversionResult", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(symbolResult);

        Type successfulArgumentConversionResultType =
          optionResultType.Assembly.GetType("System.CommandLine.Binding.SuccessfulArgumentConversionResult");

        object theValue =
          successfulArgumentConversionResultType.GetProperty("Value")?.GetValue(theArgumentConversionResult);

        Type.GetProperty(symbolResult.Symbol.Name).SetValue(request, theValue); // "Haa",9,7,"Ha"
      }

      await Mediator.Send(request);

      return 0;
    }
    catch (Exception excpetion)
    {
      Console.Error.WriteLine(excpetion.Message);
      return 1;
    }
  }
}
