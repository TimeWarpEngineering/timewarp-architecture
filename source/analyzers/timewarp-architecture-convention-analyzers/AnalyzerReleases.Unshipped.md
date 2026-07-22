; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
TWA0002 | Design | Warning | ContractNullabilityValidatorAnalyzer: nullable property has a NotEmpty()/NotNull() presence rule
TWA0003 | Design | Warning | ContractNullabilityValidatorAnalyzer: required property has a fabricated empty-string default
TWA0004 | Documentation | Warning | PurposeRegionAnalyzer: source file lacks a #region Purpose block
TWA0005 | Design | Warning | EndpointCoverageAnalyzer: endpoint HTTP verb does not match the contract's [ApiRoute] verb
TWA0006 | Design | Warning | EndpointCoverageAnalyzer: routed contract has no server endpoint
TWA0007 | Design | Warning | AspireResourceNameAnalyzer: Aspire resource name is not a ServiceNames constant value
TWA0008 | Design | Warning | TemplateConditionalTokenAnalyzer: comment or string contains a template-conditional token
TWA0009 | Design | Warning | SliceIsolationAnalyzer: slice references another product slice
TWA0010 | Design | Warning | TemplateFlagConstantsAnalyzer: directive uses a template flag missing from DefineConstants
TWA0011 | Design | Warning | AggregateInvariantsAnalyzer: aggregate root has no nested Invariants validator
TWA0012 | Design | Warning | AggregateInvariantsAnalyzer: nested Invariants validator is not private
TWA0013 | Design | Warning | EndpointAuthPostureAnalyzer: [ApiEndpoint] contract has no [EndpointAuthorize]/[EndpointAllowAnonymous] marker
TWA0014 | Design | Warning | EndpointAuthPostureAnalyzer: contract's auth posture is contradictory (both markers, or [EndpointAllowAnonymous] with IAuthApiRequest)
TWA9999 | Naming | Warning | FeatureFilenameGrammarAnalyzer (SPIKE 114-001 — not for release)
