; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
TWPA0002 | Design | Warning | ContractNullabilityValidatorAnalyzer: nullable property has a NotEmpty()/NotNull() presence rule
TWPA0003 | Design | Warning | ContractNullabilityValidatorAnalyzer: required property has a fabricated empty-string default
TWPA0004 | Documentation | Warning | PurposeRegionAnalyzer: source file lacks a #region Purpose block
TWPA0005 | Design | Warning | EndpointCoverageAnalyzer: endpoint HTTP verb does not match the contract's [ApiRoute] verb
TWPA0006 | Design | Warning | EndpointCoverageAnalyzer: routed contract has no server endpoint
TWPA0007 | Design | Warning | AspireResourceNameAnalyzer: Aspire resource name is not a ServiceNames constant value
TWPA0008 | Design | Warning | TemplateConditionalTokenAnalyzer: comment or string contains a template-conditional token
