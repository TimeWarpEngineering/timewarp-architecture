namespace TimeWarp.Architecture.Analyzer;

using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TimeWarp.Architecture.Analyzers;
using TimeWarp.Architecture.Extensions;
using TimeWarp.Architecture.Web.Spa;

public class ProjectAnlayzer
{
  public List<string> ErrorMessages =>
    PageAnalyzers.SelectMany(aPageAnalyzer => aPageAnalyzer.ErrorMessages).ToList();

  public List<PageAnalyzer> PageAnalyzers { get; }

  public ProjectAnlayzer()
  {
    PageAnalyzers = new List<PageAnalyzer>();
  }

  public void Analyze()
  {
    Assembly assembly = typeof(Program).GetTypeInfo().Assembly;
    IEnumerable<Type> pageTypes = assembly.GetTypesWithAttribute(typeof(RouteAttribute));
    foreach (Type pageType in pageTypes)
    {
      var pageAnalyzer = new PageAnalyzer(pageType);
      PageAnalyzers.Add(pageAnalyzer);
      pageAnalyzer.Analyze();
    }

    if (ErrorMessages.Count > 0)
    {
      throw new Exception(string.Join('\n', ErrorMessages));
    }
  }
}
