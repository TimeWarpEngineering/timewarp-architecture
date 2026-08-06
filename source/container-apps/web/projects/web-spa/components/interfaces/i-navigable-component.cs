#region Purpose
// Lets navigation menus read a page's title, icon, and auth policy statically, without instantiating the page.
#endregion

namespace TimeWarp.Architecture.Common.Interfaces;

public interface INavigableComponent
{
  static abstract string Title { get; }
  static abstract Icon? NavIcon { get; }
  static abstract string Policy { get; }
}
