namespace TimeWarp.Blazor.Configuration;

using System;

/// <summary>
/// The section name in appsettings.json to which the class should be mapped
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class SectionNameAttribute : Attribute
{
  public string SectionName { get; set; }
  public SectionNameAttribute(string aSectionName)
  {
    this.SectionName = aSectionName;
  }
}
