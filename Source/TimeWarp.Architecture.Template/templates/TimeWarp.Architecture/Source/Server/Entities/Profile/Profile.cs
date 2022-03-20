namespace TimeWarp.Architecture.Entities
{
  public class Profile : BaseEntity
  {
    public string DisplayName { get; set; }
    public string Language { get; set; }
    public bool Notifications { get; set; }
    public string Region { get; set; }
    public string Theme { get; set; }
  }
}
