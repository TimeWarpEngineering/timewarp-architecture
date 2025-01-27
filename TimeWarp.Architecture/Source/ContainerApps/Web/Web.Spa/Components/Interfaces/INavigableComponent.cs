namespace TimeWarp.Architecture.Common.Interfaces;

public interface INavigableComponent
{
  static abstract string Title { get; }
  static abstract Icon? NavIcon { get; }
}
