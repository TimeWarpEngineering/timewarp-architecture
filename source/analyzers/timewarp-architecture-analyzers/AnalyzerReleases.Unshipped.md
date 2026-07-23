; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
TWA0001 | Design | Warning | PartialClassDeclarationAnalyzer, [Documentation](https://github.com/TimeWarpEngineering/timewarp-architecture/blob/main/Documentation/Analyzers/TWA0001.md)
TWA0017 | Design | Warning | IngressRoutePrefixGenerator: a generated web ingress prefix shadows another server's route space (foreign contracts route or reserved prefix)
TWA0018 | Design | Warning | IngressRoutePrefixGenerator: a web-contracts route cannot be collapsed to a top-level ingress prefix (bare 'api' or parameterized second segment)
TWE001 | ApiEndpoint | Error | Endpoint class must be partial
TWE002 | ApiEndpoint | Error | Missing Query/Command class
TWE003 | ApiEndpoint | Error | Route conflict detected
TWE004 | ApiEndpoint | Error | Invalid interface implementation
TWE005 | Page | Error | [Page] Policy must be a const field reference (not string literal or nameof)
TWE006 | TypedId | Error | [TypedId] target must be a readonly partial record struct
SG010 | SourceGenerator | Warning | TypedId BCL surface generation failed (resilience backstop, names the type)
SG011 | SourceGenerator | Warning | TypedId EF converter generation failed (resilience backstop)
