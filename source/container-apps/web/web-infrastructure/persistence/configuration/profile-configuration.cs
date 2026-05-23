namespace TimeWarp.Architecture.Data.Configuration;

public class ProfileConfiguration : IEntityTypeConfiguration<Profile>
{
  public void Configure(EntityTypeBuilder<Profile> entityTypeBuilder)
  {
    var guidToStringConverter = new GuidToStringConverter();

    entityTypeBuilder
        .Property(nameof(Profile.Guid))
        .HasConversion(guidToStringConverter);

    entityTypeBuilder
        .ToContainer($"{nameof(Profile)}s")
        .HasNoDiscriminator()
        .HasPartitionKey(profile => profile.Guid)
        .HasKey(profile => profile.Guid);
  }
}
