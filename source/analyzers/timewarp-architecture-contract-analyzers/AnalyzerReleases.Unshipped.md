; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
TWPA0002 | Design | Warning | ContractNullabilityValidatorAnalyzer: nullable property has a NotEmpty()/NotNull() presence rule
TWPA0003 | Design | Warning | ContractNullabilityValidatorAnalyzer: required property has a fabricated empty-string default
