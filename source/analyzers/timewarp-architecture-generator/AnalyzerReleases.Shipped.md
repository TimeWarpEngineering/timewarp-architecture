; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 1.0.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
TWE001  | ApiEndpoint | Warning | ApiEndpoint class must be static and partial
TWE002  | ApiEndpoint | Warning | Missing Query/Command class
TWE003  | ApiEndpoint | Warning | Route conflict detected
TWE004  | ApiEndpoint | Warning | Invalid interface implementation