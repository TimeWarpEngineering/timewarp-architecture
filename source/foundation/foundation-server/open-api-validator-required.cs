#region Purpose
// Publishes the request members a validator rejects when omitted as required in the OpenAPI document.
#endregion

#region Design
// FluentValidationBehavior runs every registered IValidator<TRequest> before the handler, so the
// validator is the source of truth for which request members a caller must send. Schema export
// cannot see validator rules, so a command published every member as optional, and generated
// clients treated names, signatures, and keys as optional. A hand-written [Required] would be a
// second copy of the rule that can drift, so this file derives required from the same validators.
//
// A member is required when an unconditional NotEmpty or NotNull rule in the default rule set
// rejects the value an omitted member deserializes to. Omission leaves the default the type's
// constructor sets (JsonTypeInfo.CreateObject), so List<Guid> Ids = [] with NotNull is optional
// and string Name = null! with NotEmpty is required. NotNull on a non-nullable value type never
// fails, so it never makes a member required. Excluded: rules or components with When/Unless,
// RuleForEach (it checks the elements, not the member), rule sets other than default, and nested
// paths (Address.Street), which belong to the nested type's own schema. The member comes from
// the rule's lambda (x => x.Name), not IValidationRule.PropertyName: FastEndpoints sets
// FluentValidation's global PropertyNameResolver, which camel-cases that name. A self rule
// (RuleFor(x => x).SetValidator(...)) and Include(...) apply a child validator to the same
// object, so their rules are read too; the child comes from DI or its public parameterless
// constructor, and a child that has neither is skipped rather than failing the document.
//
// Only mediator requests (IRequest, IRequest<T>) with a registered IValidator<T> are read. Validators are
// scoped (AddValidatorsFromAssemblyContaining), so each read uses its own scope. The schema
// transformer adds required to the request body schema. The operation transformer marks the
// matching query and header parameters required (GET and DELETE members are parameters), and
// stores the required names in the operation's Metadata, which is not serialized. It is
// registered after OpenApiDeleteRequestBody so the DELETE query parameters exist.
//
// The document check runs last, after the host callback. For each operation it reads the stored
// names and throws when one of them is a body property missing from required, or a parameter not
// marked required. The throw makes /openapi/{doc}.json fail with the operation and member named,
// so a later transformer that replaces a request schema cannot silently drop the rule.
#endregion

namespace TimeWarp.Foundation;

using System.Collections;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using FluentValidation.Internal;
using FluentValidation.Validators;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

/// <summary>
/// Publishes request members that validator rules make mandatory as OpenAPI <c>required</c>.
/// </summary>
internal static class OpenApiValidatorRequired
{
  private const string MetadataKey = "timewarp-validator-required";
  private const string DefaultRuleSet = "default";

  /// <summary>
  /// Registers the request schema and parameter transformers.
  /// </summary>
  internal static void Register(OpenApiOptions openApiOptions)
  {
    ArgumentNullException.ThrowIfNull(openApiOptions);
    openApiOptions.AddSchemaTransformer(TransformSchemaAsync);
    openApiOptions.AddOperationTransformer(TransformOperationAsync);
  }

  /// <summary>
  /// Registers the document check. Call after every transformer that can rewrite a request.
  /// </summary>
  internal static void RegisterDocumentCheck(OpenApiOptions openApiOptions)
  {
    ArgumentNullException.ThrowIfNull(openApiOptions);
    openApiOptions.AddDocumentTransformer(CheckDocumentAsync);
  }

  /// <summary>
  /// JSON names of the members of <paramref name="typeInfo"/> that the registered validators
  /// reject when the member is omitted. Empty when the type is not a validated mediator request.
  /// </summary>
  internal static IReadOnlyList<string> RequiredMembers(IServiceProvider services, JsonTypeInfo typeInfo)
  {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(typeInfo);
    Type type = typeInfo.Type;
    if (!IsMediatorRequest(type) || type.IsAbstract || type.ContainsGenericParameters)
    {
      return [];
    }

    Type validatorType = typeof(IValidator<>).MakeGenericType(type);
    if (services.GetService<IServiceProviderIsService>()?.IsService(validatorType) != true)
    {
      return [];
    }

    Dictionary<string, bool> rules = new(StringComparer.Ordinal);
    using (IServiceScope scope = services.CreateScope())
    {
      HashSet<Type> visited = [];
      foreach (object? validator in scope.ServiceProvider.GetServices(validatorType))
      {
        if (validator is IEnumerable<IValidationRule> validationRules)
        {
          Collect(validationRules, scope.ServiceProvider, rules, visited);
        }
      }
    }

    if (rules.Count == 0)
    {
      return [];
    }

    object? defaults = typeInfo.CreateObject?.Invoke();
    List<string> required = [];
    foreach (JsonPropertyInfo property in typeInfo.Properties)
    {
      if (property.AttributeProvider is MemberInfo member
        && rules.TryGetValue(member.Name, out bool rejectsEmpty)
        && RejectsOmission(property, defaults, rejectsEmpty))
      {
        required.Add(property.Name);
      }
    }

    return required;
  }

  /// <summary>
  /// Adds the member name of each unconditional NotEmpty (true) or NotNull (false) rule.
  /// </summary>
  private static void Collect
  (
    IEnumerable<IValidationRule> validationRules,
    IServiceProvider services,
    Dictionary<string, bool> rules,
    HashSet<Type> visited
  )
  {
    foreach (IValidationRule rule in validationRules)
    {
      if (rule.HasCondition || rule.HasAsyncCondition || !InDefaultRuleSet(rule) || IsCollectionRule(rule))
      {
        continue;
      }

      Expression? body = Unwrap(rule.Expression?.Body);
      bool self = body is null or ParameterExpression;
      string? memberName = body is MemberExpression { Expression: ParameterExpression } access ? access.Member.Name : null;
      if (!self && memberName is null)
      {
        continue;
      }

      foreach (IRuleComponent component in rule.Components)
      {
        if (component.HasCondition || component.HasAsyncCondition)
        {
          continue;
        }

        switch (component.Validator)
        {
          case INotEmptyValidator when memberName is not null:
            rules[memberName] = true;
            break;
          case INotNullValidator when memberName is not null:
            rules.TryAdd(memberName, false);
            break;
          case IChildValidatorAdaptor child when self && visited.Add(child.ValidatorType):
            if (ChildValidator(services, child.ValidatorType) is IEnumerable<IValidationRule> childRules)
            {
              Collect(childRules, services, rules, visited);
            }

            break;
        }
      }
    }
  }

  /// <summary>
  /// The rule's lambda body without the Convert nodes a boxed or interface-typed member adds.
  /// </summary>
  private static Expression? Unwrap(Expression? expression)
  {
    while (expression is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } conversion)
    {
      expression = conversion.Operand;
    }

    return expression;
  }

  private static object? ChildValidator(IServiceProvider services, Type validatorType)
  {
    if (validatorType.IsAbstract || validatorType.IsInterface || validatorType.ContainsGenericParameters)
    {
      return null;
    }

    return services.GetService(validatorType)
      ?? (validatorType.GetConstructor(Type.EmptyTypes) is null ? null : Activator.CreateInstance(validatorType));
  }

  private static bool IsMediatorRequest(Type type) =>
    typeof(TimeWarp.Mediator.IRequest).IsAssignableFrom(type)
      || type.GetInterfaces()
        .Any(static contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(TimeWarp.Mediator.IRequest<>));

  private static bool InDefaultRuleSet(IValidationRule rule) =>
    rule.RuleSets is null
      || rule.RuleSets.Length == 0
      || rule.RuleSets.Contains(DefaultRuleSet, StringComparer.OrdinalIgnoreCase);

  private static bool IsCollectionRule(IValidationRule rule) =>
    rule.GetType().GetInterfaces()
      .Any(static contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(ICollectionRule<,>));

  /// <summary>
  /// True when the value an omitted member keeps fails the rule: null always, and empty under NotEmpty.
  /// </summary>
  private static bool RejectsOmission(JsonPropertyInfo property, object? defaults, bool rejectsEmpty)
  {
    if (defaults is null || property.Get is null)
    {
      // No default instance to read: an omitted reference or Nullable<T> member is null.
      return rejectsEmpty || !property.PropertyType.IsValueType || Nullable.GetUnderlyingType(property.PropertyType) is not null;
    }

    object? value = property.Get(defaults);
    return value is null || (rejectsEmpty && IsEmpty(value));
  }

  /// <summary>
  /// FluentValidation's NotEmpty: whitespace string, empty collection, or the type's default value.
  /// </summary>
  private static bool IsEmpty(object value) =>
    value switch
    {
      string text => string.IsNullOrWhiteSpace(text),
      ICollection collection => collection.Count == 0,
      IEnumerable sequence => !sequence.GetEnumerator().MoveNext(),
      _ => value.GetType().IsValueType && value.Equals(Activator.CreateInstance(value.GetType()))
    };

  private static Task TransformSchemaAsync
  (
    OpenApiSchema schema,
    OpenApiSchemaTransformerContext context,
    CancellationToken cancellationToken
  )
  {
    _ = cancellationToken;
    IReadOnlyList<string> required = RequiredMembers(context.ApplicationServices, context.JsonTypeInfo);
    foreach (string name in required)
    {
      if (schema.Properties?.ContainsKey(name) == true)
      {
        schema.Required ??= new HashSet<string>(StringComparer.Ordinal);
        schema.Required.Add(name);
      }
    }

    return Task.CompletedTask;
  }

  private static Task TransformOperationAsync
  (
    OpenApiOperation operation,
    OpenApiOperationTransformerContext context,
    CancellationToken cancellationToken
  )
  {
    _ = cancellationToken;
    Type? requestType = context.Description.ActionDescriptor.EndpointMetadata
      .OfType<EndpointDefinition>()
      .FirstOrDefault()?
      .ReqDtoType;
    if (requestType is not { ContainsGenericParameters: false })
    {
      return Task.CompletedTask;
    }

    JsonSerializerOptions serializerOptions =
      context.ApplicationServices.GetService<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>()?.Value.SerializerOptions
      ?? JsonSerializerOptions.Web;
    if (!serializerOptions.TryGetTypeInfo(requestType, out JsonTypeInfo? typeInfo))
    {
      return Task.CompletedTask;
    }

    IReadOnlyList<string> required = RequiredMembers(context.ApplicationServices, typeInfo);
    if (required.Count == 0)
    {
      return Task.CompletedTask;
    }

    operation.Metadata ??= new Dictionary<string, object>(StringComparer.Ordinal);
    operation.Metadata[MetadataKey] = required;
    foreach (IOpenApiParameter parameter in operation.Parameters ?? [])
    {
      if (parameter is OpenApiParameter { In: ParameterLocation.Query or ParameterLocation.Header } concrete
        && required.Contains(concrete.Name, StringComparer.OrdinalIgnoreCase))
      {
        concrete.Required = true;
      }
    }

    return Task.CompletedTask;
  }

  private static Task CheckDocumentAsync
  (
    OpenApiDocument document,
    OpenApiDocumentTransformerContext context,
    CancellationToken cancellationToken
  )
  {
    _ = context;
    _ = cancellationToken;
    List<string> wrong = [];
    foreach (KeyValuePair<string, IOpenApiPathItem> path in document.Paths ?? [])
    {
      foreach (KeyValuePair<HttpMethod, OpenApiOperation> operation in path.Value.Operations ?? [])
      {
        if (operation.Value.Metadata?.TryGetValue(MetadataKey, out object? value) != true
          || value is not IReadOnlyList<string> required)
        {
          continue;
        }

        List<IOpenApiSchema> bodies = [.. BodySchemas(document, operation.Value)];
        foreach (string name in required)
        {
          if (!IsPublishedRequired(operation.Value, bodies, name))
          {
            wrong.Add($"{operation.Key} {path.Key} {name}");
          }
        }
      }
    }

    if (wrong.Count > 0)
    {
      throw new InvalidOperationException
      (
        "OpenApiValidatorRequired: these request members have an unconditional NotEmpty or NotNull rule "
        + "but are not required in the document: "
        + string.Join(", ", wrong)
        + ". A transformer that replaces a request schema or parameter must keep required."
      );
    }

    return Task.CompletedTask;
  }

  /// <summary>
  /// True when <paramref name="name"/> is required in every body schema that has it, or is a
  /// required parameter. A member published in neither place (route-bound, ignored) passes.
  /// </summary>
  private static bool IsPublishedRequired(OpenApiOperation operation, List<IOpenApiSchema> bodies, string name)
  {
    bool inBody = false;
    foreach (IOpenApiSchema body in bodies)
    {
      if (body.Properties?.ContainsKey(name) == true)
      {
        inBody = true;
        if (body.Required?.Contains(name) != true)
        {
          return false;
        }
      }
    }

    IOpenApiParameter? parameter = (operation.Parameters ?? [])
      .FirstOrDefault(candidate => string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase));
    return parameter?.Required != false || inBody;
  }

  private static IEnumerable<IOpenApiSchema> BodySchemas(OpenApiDocument document, OpenApiOperation operation)
  {
    foreach (IOpenApiMediaType mediaType in operation.RequestBody?.Content?.Values ?? [])
    {
      IOpenApiSchema? schema = mediaType.Schema;
      if (schema is OpenApiSchemaReference reference
        && reference.Reference.Id is string id
        && document.Components?.Schemas?.TryGetValue(id, out IOpenApiSchema? component) == true)
      {
        schema = component;
      }

      if (schema is not null)
      {
        yield return schema;
      }
    }
  }
}
