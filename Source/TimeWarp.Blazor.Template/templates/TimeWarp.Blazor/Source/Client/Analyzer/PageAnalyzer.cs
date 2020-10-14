namespace TimeWarp.Blazor.Analyzers
{
  using Microsoft.AspNetCore.Components;
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Reflection;

  public class PageAnalyzer
  {
    private const string RouteTemplateName = "RouteTemplate";
    private readonly Type PageType;

    private string PageName => PageType.Name;
    public List<string> ErrorMessages { get; }

    public PageAnalyzer(Type aPageType)
    {
      PageType = aPageType;
      ErrorMessages = new List<string>();
    }

    public void Analyze()
    {
      EnsurePageHasGetRouteMethod();
      EnsureRouteTemplateMatchesRouteAttribute();
    }

    private void EnsurePageHasGetRouteMethod()
    {
      MethodInfo[] methodInfos = PageType.GetMethods();
      string getRouteName = nameof(Pages.Index.GetRoute);
      if (!methodInfos.Any(aMethodInfo => aMethodInfo.Name == getRouteName && aMethodInfo.IsStatic && aMethodInfo.IsPublic))
      {
        string message = $"The page named `{PageName}` is missing a `public static string {getRouteName}` method.";
        ErrorMessages.Add(message);
      }
    }

    /// <summary>
    /// Ensure the RouteTemplate private const matches the RouteAttribute.Template value;
    /// </summary>
    private void EnsureRouteTemplateMatchesRouteAttribute()
    {
      FieldInfo fieldInfo = EnsureRouteTemplateFieldExists();

      if (fieldInfo != null)
      {
        EnsureRouteTemplateValueMatchesRouteAttributeTemplate(fieldInfo);
      }


      FieldInfo EnsureRouteTemplateFieldExists()
      {
        FieldInfo fieldInfo = PageType.GetFields(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy)
          .Where(aFieldInfo => aFieldInfo.IsLiteral && !aFieldInfo.IsInitOnly)
          .SingleOrDefault(aFieldInfo => aFieldInfo.Name == RouteTemplateName);
        if (fieldInfo == null)
        {
          string message = $"The page named `{PageName}` is missing a `private const string {RouteTemplateName}`.";
          ErrorMessages.Add(message);
        }

        return fieldInfo;
      }

      void EnsureRouteTemplateValueMatchesRouteAttributeTemplate(FieldInfo aFieldInfo)
      {
        // Then ensure its value matches the RouteAttribute.Teamplate
        string routeTemplateValue = aFieldInfo.GetValue(null).ToString();
        var routeAttribute = PageType.GetCustomAttribute(typeof(RouteAttribute)) as RouteAttribute;
        if (routeAttribute.Template != routeTemplateValue)
        {
          string message = $"The page named `{PageName}` has `@page` value of `{routeAttribute.Template}` which does not match the `private const string {RouteTemplateName}` value of `{routeTemplateValue}`.";
          ErrorMessages.Add(message);
        }
      }
    }
  }
}
