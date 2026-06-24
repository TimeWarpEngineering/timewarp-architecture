; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
TWPA0001 | Design | Warning | PartialClassDeclarationAnalyzer, [Documentation](https://github.com/TimeWarpEngineering/timewarp-architecture/blob/main/Documentation/Analyzers/TWPA0001.md)
TWE001 | ApiEndpoint | Error | Endpoint class must be partial
TWE002 | ApiEndpoint | Error | Missing Query/Command class
TWE003 | ApiEndpoint | Error | Route conflict detected
TWE004 | ApiEndpoint | Error | Invalid interface implementation
