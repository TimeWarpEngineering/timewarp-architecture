#region Purpose
// One principal's profile as an administrator sees it through IProfileDirectory.
#endregion

#region Design
// Task 328. Every field except PrincipalId is personal data (Email is PII), so callers must
// gate a read on admin.profiles.read. The record is a read-only snapshot of the profile
// aggregate, not the aggregate itself, so the Admin slice never references the Profile slice
// (TWA0009).
#endregion

namespace TimeWarp.Architecture.Abstractions;

public sealed record ProfileDirectoryEntry(
  Guid PrincipalId,
  string DisplayName,
  string? Email,
  string Language,
  string Region,
  string Theme,
  bool Notifications);
