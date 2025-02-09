# 007_Fix-Source-Generator-Output-Location.md

## Description

Update the FastEndpointSourceGenerator to generate endpoint implementations in the Api.Server project based on attributes defined in Api.Contracts project.

## Requirements

1. Move source generator output from Api.Contracts to Api.Server project
2. Maintain attribute definitions in Api.Contracts
3. Generator must scan referenced assemblies for endpoint attributes
4. Generated endpoints must be accessible in Api.Server

## Checklist

### Design
- [ ] Review current source generator implementation
- [ ] Design assembly scanning approach
- [ ] Plan project reference structure

### Implementation
- [ ] Move analyzer reference from Api.Contracts to Api.Server.csproj
- [ ] Configure output path in Api.Server's MSBuild properties
- [ ] Update FastEndpointSourceGenerator to scan referenced assemblies
- [ ] Implement cross-assembly type symbol resolution
- [ ] Update generator to output files in Api.Server/Generated
- [ ] Add/update necessary unit tests

### Documentation
- [ ] Update source generator documentation
- [ ] Document project reference requirements
- [ ] Document generator configuration options

### Review
- [ ] Verify generated endpoints are correctly placed
- [ ] Ensure build process works correctly
- [ ] Test with multiple endpoint definitions
- [ ] Code Review

## Notes

Technical Implementation Details:
1. Api.Server.csproj changes:
```xml
<ItemGroup>
  <ProjectReference Include="..\..\Analyzers\TimeWarp.Architecture.SourceGenerator\TimeWarp.Architecture.SourceGenerator.csproj" 
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false"/>
</ItemGroup>
<PropertyGroup>
  <GeneratedFolder>Generated</GeneratedFolder>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>$(GeneratedFolder)</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

2. Generator Updates:
```csharp
context.RegisterSourceOutput(context.CompilationProvider, (productionContext, compilation) => 
{
    foreach (INamedTypeSymbol type in compilation.SourceModule.ReferencedAssemblySymbols
             .SelectMany(a => a.GetTypeByMetadataName("Api.Contracts")?.GetMembers()))
    {
        if (HasEndpointAttribute(type))
        {
            GenerateServerImplementation(type, productionContext);
        }
    }
});
```

## Implementation Notes

Current State:
- Generator outputs to Api.Contracts/Generated
- Attributes defined in Api.Contracts
- Need to move generation to Api.Server while keeping attributes in Api.Contracts
