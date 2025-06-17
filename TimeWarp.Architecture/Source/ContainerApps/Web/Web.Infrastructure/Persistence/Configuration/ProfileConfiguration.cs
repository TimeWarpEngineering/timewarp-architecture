namespace TimeWarp.Architecture.Data.Configuration;

public class ProfileConfiguration : IEntityTypeConfiguration<Profile>
{
  public void Configure(EntityTypeBuilder<Profile> aEntityTypeBuilder)
  {
    var guidToStringConverter = new GuidToStringConverter();

    aEntityTypeBuilder
        .Property(nameof(Profile.Guid))
        .HasConversion(guidToStringConverter);

    aEntityTypeBuilder
        .ToContainer($"{nameof(Profile)}s")
        .HasNoDiscriminator()
        .HasPartitionKey(aProfile => aProfile.Guid)
        .HasKey(aProfile => aProfile.Guid);
  }
}
