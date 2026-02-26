# Fix FastEndpointSourceGenerator

## Status: Failed

## Problem
The FastEndpointSourceGenerator is not detecting and processing the GetWeatherForecasts class from Api.Contracts.

## Attempted Solutions
1. Added detailed diagnostic logging to understand what files are being scanned
2. Modified project references to make Api.Contracts visible to the source generator:
   ```xml
   <ProjectReference Include="..\Api.Contracts\Api.Contracts.csproj" />
   ```
3. Switched to using ForAttributeWithMetadataName for more precise attribute detection:
   ```csharp
   context.SyntaxProvider.ForAttributeWithMetadataName(
       ApiEndpointAttributeFullName,
       predicate: (node, _) => node is ClassDeclarationSyntax cds &&
           cds.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)) &&
           cds.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)),
       transform: (context, _) => ((ClassDeclarationSyntax)context.TargetNode, context.SemanticModel))
   ```

## Current State
- Source generator initializes successfully
- Api.Contracts.dll is referenced
- But GetWeatherForecasts class is not being found in the syntax trees
- Diagnostic output shows only Api.Server files being scanned

## Next Steps
Need expert help to understand:
1. Why Api.Contracts source files aren't being included in the compilation
2. How to properly configure source generators to work with referenced projects
3. Whether there's a fundamental issue with our source generator approach

## Notes
- The source generator sees the assembly references but not the source files
- Diagnostic output shows 389 referenced assemblies but only Api.Server source files
- Current approach is not working despite multiple attempts at fixes
