#region Purpose
// The app's data classification taxonomy: which kinds of values must never reach logs or OTel export as-is.
#endregion

#region Design
// Task 344. Two classes, kept deliberately small: PersonalData (identifies a person — email, name,
// phone, address, account or client numbers) and SensitiveData (free text or secrets a person
// supplied — feedback bodies, notes, tokens). Both map to the erasing redactor in AddLogRedaction,
// so a separate class only matters when a team later wants a different redactor (for example an
// HMAC redactor for PersonalData so records stay correlatable). Unclassified values log unchanged.
// Lives in service defaults because every server host references it and no CLI does; server-side
// code tags [LoggerMessage] parameters or [LogProperties] members with PersonalDataAttribute or
// SensitiveDataAttribute.
#endregion

namespace TimeWarp.Architecture.Compliance;

using Microsoft.Extensions.Compliance.Classification;

public static class DataTaxonomy
{
  public static string TaxonomyName { get; } = typeof(DataTaxonomy).FullName!;

  public static DataClassification PersonalData { get; } = new(TaxonomyName, nameof(PersonalData));

  public static DataClassification SensitiveData { get; } = new(TaxonomyName, nameof(SensitiveData));
}
