#region Purpose
// Shared harness for the TWA0039–TWA0043 contract-agreement tests: platform stubs, the Roles and
// Profile shapes (contracts and domain as separate projects), and the Test/Flag builders.
#endregion

#region Design
// The analyzer reads both sides as metadata in web-application, so the harness mirrors that
// layout: a Foundation project (IAggregateRoot, Entity, OperationAttribute, mediator kinds, OneOf,
// BaseResponse, ListResponse, the marker attributes), a Contracts project and a Domain project
// that each reference Foundation, and the test's main project (the "application") that references
// all three. Every diagnostic is then Location.None, as in the real build. Each test passes the
// contracts and domain sources it needs; the Roles and Profile constants are the template's shapes
// after task 474 (Permissions, DisplayName), so a test that wants drift edits a copy.
#endregion

namespace TimeWarp.Architecture.Analyzers.Tests;

using Microsoft.CodeAnalysis.CSharp.Testing;

internal static class ContractAgreementTestFixture
{
  internal const string Foundation =
    """
    #nullable enable
    using System;
    namespace TimeWarp.Foundation.Entities
    {
      public interface IAggregateRoot { }
      public abstract class Entity<TId>
      {
        protected Entity(TId id) { Id = id; }
        public TId Id { get; }
        public int Version { get; private set; }
      }
    }
    namespace TimeWarp.Foundation.Features
    {
      public abstract class BaseResponse { public Guid CorrelationId { get; set; } }
      public abstract class ListResponse<T>
      {
        protected ListResponse(int totalCount, T[] items) { TotalCount = totalCount; Items = items; }
        public int TotalCount { get; }
        public T[] Items { get; }
      }
    }
    namespace TimeWarp.Contracts
    {
      [AttributeUsage(AttributeTargets.Class)]
      public sealed class OperationAttribute : Attribute
      {
        public OperationAttribute(string name) { Name = name; }
        public string Name { get; }
        public string? Description { get; set; }
        public string? Group { get; set; }
      }
    }
    namespace TimeWarp.Mediator
    {
      public interface IRequest<T> { }
      public interface IQuery<T> : IRequest<T> { }
      public interface ICommand<T> : IRequest<T> { }
      public interface IIdempotentCommand<T> : IRequest<T> { }
    }
    namespace OneOf
    {
      public class OneOf<T0, T1> { }
    }
    namespace TimeWarp.Architecture.Attributes
    {
      public sealed class NotAggregateAttribute : Attribute
      {
        public NotAggregateAttribute(string reason) { Reason = reason; }
        public string Reason { get; }
      }
      public sealed class ComputedAttribute : Attribute { }
    }
    namespace TimeWarp.Foundation
    {
      public sealed class SharedProblemDetails { }
      public interface IAuthApiRequest { Guid UserId { get; set; } }
    }
    """;

  internal const string ContractsUsings =
    """
    #nullable enable
    using System;
    using System.Collections.Generic;
    using OneOf;
    using TimeWarp.Architecture.Attributes;
    using TimeWarp.Contracts;
    using TimeWarp.Foundation;
    using TimeWarp.Foundation.Features;
    using TimeWarp.Mediator;

    """;

  internal const string DomainUsings =
    """
    #nullable enable
    using System;
    using System.Collections.Generic;
    using TimeWarp.Foundation.Entities;

    """;

  internal const string RoleContracts =
    """
    namespace App.Features.Admin.Roles
    {
      public interface IRoleDetails
      {
        string Name { get; set; }
        string Description { get; set; }
      }

      public static class CreateRole
      {
        [Operation("roles.create", Group = "admin.roles")]
        public sealed class Command : IAuthApiRequest, IRoleDetails, ICommand<OneOf<Response, SharedProblemDetails>>
        {
          public Guid UserId { get; set; }
          public string Name { get; set; } = "";
          public string Description { get; set; } = "";
        }
        public sealed class Response { public Guid RoleId { get; set; } }
      }

      public static class UpdateRole
      {
        [Operation("roles.update", Group = "admin.roles")]
        public sealed class Command : IAuthApiRequest, IRoleDetails, IIdempotentCommand<OneOf<Response, SharedProblemDetails>>
        {
          public Guid UserId { get; set; }
          public Guid RoleId { get; set; }
          public string Name { get; set; } = "";
          public string Description { get; set; } = "";
        }
        public sealed class Response { }
      }

      public static class SetRolePermissions
      {
        [Operation("roles.set-permissions", Group = "admin.roles")]
        public sealed class Command : IAuthApiRequest, IIdempotentCommand<OneOf<Response, SharedProblemDetails>>
        {
          public Guid UserId { get; set; }
          public Guid RoleId { get; set; }
          public List<string> PermissionIds { get; set; } = [];
        }
        public sealed class Response { public List<string> PermissionIds { get; set; } = []; }
      }

      public static class SetRoleAgentScopes
      {
        [Operation("roles.set-agent-scopes", Group = "admin.roles")]
        public sealed class Command : IAuthApiRequest, IIdempotentCommand<OneOf<Response, SharedProblemDetails>>
        {
          public Guid UserId { get; set; }
          public Guid RoleId { get; set; }
          public List<string> Scopes { get; set; } = [];
        }
        public sealed class Response { }
      }

      public static class DeleteRole
      {
        [Operation("roles.delete", Group = "admin.roles")]
        [NotAggregate("Removes the role; only the route id travels.")]
        public sealed class Command : IAuthApiRequest, IIdempotentCommand<OneOf<Response, SharedProblemDetails>>
        {
          public Guid UserId { get; set; }
          public Guid RoleId { get; set; }
        }
        public sealed class Response { }
      }

      public static class GetRole
      {
        [Operation("roles.get", Group = "admin.roles")]
        public sealed class Query : IAuthApiRequest, IQuery<OneOf<Response, SharedProblemDetails>>
        {
          public Guid UserId { get; set; }
          public Guid RoleId { get; set; }
        }
        public sealed class Response : IRoleDetails
        {
          public Guid RoleId { get; set; }
          public string Name { get; set; } = "";
          public string Description { get; set; } = "";
          public List<string> Permissions { get; set; } = [];
          public List<string> AgentScopes { get; set; } = [];
        }
      }

      public static class GetRoles
      {
        [Operation("roles.list", Group = "admin.roles")]
        public sealed class Query : IQuery<OneOf<Response, SharedProblemDetails>> { public string? Search { get; set; } }
        public sealed class Response : ListResponse<RoleDto> { public Response(int count, RoleDto[] items) : base(count, items) { } }
        public sealed class RoleDto
        {
          public Guid RoleId { get; set; }
          public string Name { get; set; } = "";
          public string Description { get; set; } = "";
          public List<string> Permissions { get; set; } = [];
          public List<string> AgentScopes { get; set; } = [];
        }
      }
    }
    """;

  internal const string RoleDomain =
    """
    namespace App.Features.Admin.Roles.Domain
    {
      public readonly record struct RoleId(Guid Value) { public bool IsEmpty => Value == Guid.Empty; }
      public sealed record PermissionGrant(string PermissionId);
      public sealed record AgentScopeGrant(string Scope);

      public sealed class Role : Entity<RoleId>, IAggregateRoot
      {
        private readonly List<PermissionGrant> PermissionList = [];
        private readonly List<AgentScopeGrant> AgentScopeList = [];
        private Role(RoleId id) : base(id) { }
        public string Name { get; private set; } = "";
        public string Description { get; private set; } = "";
        public IReadOnlyList<PermissionGrant> Permissions => PermissionList;
        public IReadOnlyList<AgentScopeGrant> AgentScopes => AgentScopeList;
        public void UpdateDetails(string name, string description) { Name = name; Description = description; }
        public void SetPermissions(IEnumerable<string> permissionIds) { }
        public void SetAgentScopes(IEnumerable<string> scopes) { }
      }
    }
    """;

  internal const string ProfileContracts =
    """
    namespace App.Features.Profiles
    {
      public interface IProfileDetails
      {
        string DisplayName { get; set; }
        string? Email { get; set; }
        string Theme { get; set; }
        bool Notifications { get; set; }
      }

      public static class GetProfile
      {
        [Operation("profile.get", Group = "profile")]
        public sealed class Query : IQuery<OneOf<Response, SharedProblemDetails>> { }
        public sealed class Response : BaseResponse, IProfileDetails
        {
          public string DisplayName { get; set; } = "";
          public string? Email { get; set; }
          public string Theme { get; set; } = "";
          public bool Notifications { get; set; }
          [Computed]
          public string Avatar { get; set; } = "";
        }
      }

      public static class UpdateProfile
      {
        [Operation("profile.update", Group = "profile")]
        public sealed class Command : IProfileDetails, ICommand<OneOf<Response, SharedProblemDetails>>
        {
          public string DisplayName { get; set; } = "";
          public string? Email { get; set; }
          public string Theme { get; set; } = "";
          public bool Notifications { get; set; }
        }
        public sealed class Response { }
      }
    }
    namespace App.Features.AgentChats
    {
      public static class SetUserXaiApiKey
      {
        [Operation("user-xai-api-key.set", Group = "profile")]
        [NotAggregate("The key is a protected secret row, not Profile state.")]
        public sealed class Command : IIdempotentCommand<OneOf<Response, SharedProblemDetails>> { public string ApiKey { get; set; } = ""; }
        public sealed class Response { public bool Configured { get; set; } }
      }

      public static class GetUserXaiApiKeyStatus
      {
        [Operation("user-xai-api-key.get", Group = "profile")]
        [NotAggregate("The status reads a protected secret row, not Profile state.")]
        public sealed class Query : IQuery<OneOf<Response, SharedProblemDetails>> { }
        public sealed class Response { public bool Configured { get; set; } }
      }
    }
    """;

  internal const string ProfileDomain =
    """
    namespace App.Features.Profiles.Domain
    {
      public readonly record struct ProfileId(Guid Value);

      public sealed class Profile : Entity<ProfileId>, IAggregateRoot
      {
        private Profile(ProfileId id) : base(id) { }
        public string DisplayName { get; private set; } = "";
        public string? Email { get; private set; }
        public string Theme { get; private set; } = "";
        public bool Notifications { get; private set; }
        public void Rename(string displayName) { DisplayName = displayName; }
      }
    }
    """;

  internal static CSharpAnalyzerTest<ContractAgreementAnalyzer, RoslynTestVerifier> Test(
    string contracts,
    string domain,
    params DiagnosticResult[] expected) =>
    Test([contracts], [domain], expected);

  internal static CSharpAnalyzerTest<ContractAgreementAnalyzer, RoslynTestVerifier> Test(
    string[] contracts,
    string[] domain,
    params DiagnosticResult[] expected)
  {
    CSharpAnalyzerTest<ContractAgreementAnalyzer, RoslynTestVerifier> test = new();
    test.TestState.Sources.Add(("Application.cs", "namespace App.Application { public static class Marker { } }"));

    ProjectState foundation = test.TestState.AdditionalProjects["Foundation"];
    foundation.Sources.Add(("Foundation.cs", Foundation));

    ProjectState contractsProject = test.TestState.AdditionalProjects["Contracts"];
    contractsProject.AdditionalProjectReferences.Add("Foundation");
    for (int i = 0; i < contracts.Length; i++)
    {
      contractsProject.Sources.Add(($"Contracts{i}.cs", ContractsUsings + contracts[i]));
    }

    ProjectState domainProject = test.TestState.AdditionalProjects["Domain"];
    domainProject.AdditionalProjectReferences.Add("Foundation");
    for (int i = 0; i < domain.Length; i++)
    {
      domainProject.Sources.Add(($"Domain{i}.cs", DomainUsings + domain[i]));
    }

    test.TestState.AdditionalProjectReferences.AddRange(["Foundation", "Contracts", "Domain"]);
    test.ExpectedDiagnostics.AddRange(expected);
    return test;
  }

  internal static DiagnosticResult Flag(string id, params object[] arguments)
  {
    DiagnosticSeverity severity =
      id is ContractAgreementAnalyzer.UncoveredCommandId or ContractAgreementAnalyzer.AmbiguousRootId
        ? DiagnosticSeverity.Warning
        : DiagnosticSeverity.Error;
    return new DiagnosticResult(id, severity).WithArguments(arguments);
  }
}
