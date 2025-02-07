namespace TimeWarp.Architecture.SourceGenerator;

using Models;

[Generator]
public class FastEndpointSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Reset route registry at the start of each generation
        RouteRegistry.Reset();

        // Register the attribute source
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "ApiEndpointAttribute.g.cs",
            SourceText.From(@"
using System;
namespace TimeWarp.Architecture.SourceGenerator
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ApiEndpointAttribute : Attribute
    {
        public Type? EndpointType { get; set; }
    }
}", Encoding.UTF8)));

        // Get all class declarations with the ApiEndpoint attribute
        IncrementalValuesProvider<(ClassDeclarationSyntax ClassDeclaration, SemanticModel SemanticModel)> classDeclarations = 
            context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                    transform: static (ctx, ct) => GetSemanticTargetForGeneration(ctx))
                .Where(static t => t.ClassDeclaration is not null)
                .Select(static (t, _) => (t.ClassDeclaration!, t.SemanticModel));

        // Generate the source
        context.RegisterSourceOutput(classDeclarations,
            static (spc, source) => Execute(source.ClassDeclaration, source.SemanticModel, spc));
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node) =>
        node is ClassDeclarationSyntax { AttributeLists.Count: > 0 } classDeclaration &&
        classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)) &&
        classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));

    private static (ClassDeclarationSyntax? ClassDeclaration, SemanticModel SemanticModel) GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        SemanticModel semanticModel = context.SemanticModel;

        foreach (AttributeListSyntax attributeList in classDeclaration.AttributeLists)
        {
            foreach (AttributeSyntax attribute in attributeList.Attributes)
            {
                if (semanticModel.GetSymbolInfo(attribute).Symbol is not IMethodSymbol attributeSymbol)
                {
                    continue;
                }

                INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                string fullName = attributeContainingTypeSymbol.ToDisplayString();

                if (fullName == "TimeWarp.Architecture.SourceGenerator.ApiEndpointAttribute")
                {
                    return (classDeclaration, semanticModel);
                }
            }
        }

        return (null, semanticModel);
    }

    private static void Execute(ClassDeclarationSyntax classDeclaration, SemanticModel semanticModel, SourceProductionContext context)
    {
        // Extract metadata
        var metadata = EndpointMetadata.FromSyntax(classDeclaration, semanticModel);

        // Validate the class structure
        if (!ValidateClassStructure(classDeclaration, context))
        {
            return;
        }

        // Check for route conflicts
        if (!RouteRegistry.TryRegisterRoute(metadata.Route, metadata.HttpVerb, metadata.ClassName, context))
        {
            return;
        }

        // Generate the endpoint class
        string endpointClass = GenerateEndpointClass(metadata);
        context.AddSource($"{metadata.ClassName}Endpoint.g.cs", SourceText.From(endpointClass, Encoding.UTF8));
    }

    private static bool ValidateClassStructure(ClassDeclarationSyntax classDeclaration, SourceProductionContext context)
    {
        // Check for static and partial modifiers
        if (!classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)) ||
            !classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.ApiEndpointMissingPartial,
                    classDeclaration.GetLocation(),
                    classDeclaration.Identifier.Text));
            return false;
        }

        // Check for Query/Command class
        ClassDeclarationSyntax? queryClass = classDeclaration.Members
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.Text is "Query" or "Command");

        if (queryClass is null)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.ApiEndpointMissingQuery,
                    classDeclaration.GetLocation(),
                    classDeclaration.Identifier.Text));
            return false;
        }

        // Validate interface implementations
        if (!ValidateQueryInterfaces(queryClass, context))
        {
            return false;
        }

        return true;
    }

    private static bool ValidateQueryInterfaces(ClassDeclarationSyntax queryClass, SourceProductionContext context)
    {
        string[] requiredInterfaces = new[] { "IRequest<>", "IQueryStringRouteProvider" };
        string[] implementedInterfaces = queryClass.BaseList?.Types
            .Select(t => t.Type.ToString())
            .ToArray() ?? Array.Empty<string>();

        foreach (string requiredInterface in requiredInterfaces)
        {
            if (!implementedInterfaces.Any(i => i.StartsWith(requiredInterface)))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.ApiEndpointInvalidInterface,
                        queryClass.GetLocation(),
                        requiredInterface));
                return false;
            }
        }

        return true;
    }

    private static string GenerateEndpointClass(EndpointMetadata metadata)
    {
        var builder = new StringBuilder();
        builder.AppendLine(@"using FastEndpoints;
using OneOf;
using System.Threading;
using System.Threading.Tasks;");

        builder.AppendLine($@"
namespace {metadata.Namespace};

/// <summary>
/// {metadata.Summary}
/// </summary>
/// <remarks>
/// {metadata.Description}
/// </remarks>
public class {metadata.ClassName}Endpoint : {metadata.CustomEndpointType?.FullName ?? "BaseFastEndpoint"}<{metadata.ClassName}.Query, {metadata.ClassName}.Response>
{{
    public override void Configure()
    {{
        {metadata.HttpVerb}(""{metadata.Route}"");
");

        if (metadata.RequiresAuthorization)
        {
            builder.AppendLine("        RequireAuthorization();");
        }

        if (metadata.Tags.Any())
        {
            builder.AppendLine($"        Tags({string.Join(", ", metadata.Tags.Select(t => $"\"{t}\""))});");
        }

        if (!string.IsNullOrEmpty(metadata.Summary))
        {
            builder.AppendLine($@"        Summary(s =>
        {{
            s.Summary = ""{metadata.Summary}"";
            s.Description = ""{metadata.Description}"";
            s.ExampleRequest = new {metadata.ClassName}.Query {{ Days = 5 }};
        }});

        Description(d => d
            .Produces<{metadata.ClassName}.Response>(200, ""Success"")
            .ProducesProblem(400, ""Bad Request"")
        );");
        }

        builder.AppendLine(@"    }

    public override async Task HandleAsync(Query request, CancellationToken ct)
    {
        // Implementation will be provided by the user
        throw new NotImplementedException();
    }
}");

        return builder.ToString();
    }
}