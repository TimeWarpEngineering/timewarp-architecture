namespace TimeWarp.Architecture.Common.Interfaces;

using TimeWarp.Architecture;

public interface INavigableComponent
{
  static abstract string Title { get; }
  static abstract Icon? NavIcon { get; }
  static abstract string Policy { get; }
}
