#region Purpose
// Client-only contract for the signed-in user's module and role grants (SPA mock demos).
#endregion

#region Design
// Not the identity "who am I" read — that is GetCurrentSession (cookie session / PrincipalId).
// This shape is application authorization grants (modules + roles) for SPA menus and route guards.
// Modules and Roles are Guids drawn from ModuleIds/RoleIds so client code keys on stable ids
// instead of matching role-name strings.
// The mock factory returns per-user responses keyed by MockUserIds, letting the SPA demonstrate
// role-driven UI without any grants backend; unknown users get full access because the mock
// optimizes for demo friction, not security.
// [ClientOnlyContract]: no server endpoint and no YARP ingress prefix (task 107/ingress tests).
// Rehomed under Features.Identity (task 104-021) so web/features no longer carries a near-empty
// authentication/ peer of identity/.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

public static partial class GetCurrentUser
{
  [ApiRoute(RouteTemplate: "api/GetCurrentUser", HttpVerb.Get)]
  [ClientOnlyContract("Served by SPA mock mode; no server grants endpoint in the template.")]
  public sealed partial class Query : IAuthApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public Guid UserId { get; set; }
  }

  public sealed class Validator : AbstractValidator<Query>
  {
    public Validator()
    {
      RuleFor(x => x.UserId).NotEmpty().NotEqual(Guid.Empty);
    }
  }

  public sealed class Response
  {
    public Response
    (
      List<Guid> modules,
      List<Guid> roles
    )
    {
      Modules = modules;
      Roles = roles;
    }
    /// <summary>
    /// List of Module Ids the current user has access to.
    /// </summary>
    /// <remarks> Should be from the ModuleIds</remarks>
    public List<Guid> Modules { get; init; }

    /// <summary>
    /// List of Roles to which the current user belongs
    /// </summary>
    /// <remarks>Should be from RoleIds</remarks>
    public List<Guid> Roles { get; init; }
  }

  private static readonly List<Guid> AllModules =
  [
    ModuleIds.GeneralLedger,
    ModuleIds.AccountsPayable,
    ModuleIds.AccountsReceivable,
    ModuleIds.CashManagement,
    ModuleIds.AssetManagement,
    ModuleIds.InventoryManagement,
    ModuleIds.Purchasing,
    ModuleIds.SalesAndRevenueManagement,
    ModuleIds.ExpenseManagement,
    ModuleIds.BudgetingAndForecasting,
    ModuleIds.TaxManagement,
    ModuleIds.FinancialReportingAndAnalysis,
    ModuleIds.AuditTrailsAndCompliance,
    ModuleIds.MultiCurrencyAndGlobalOperations,
    ModuleIds.Payroll,
    ModuleIds.UserAccessManagement
  ];

  public static MockResponseFactory<Response> GetMockResponseFactory()
  {
    return CreateMockResponse;
  }

  private static Response CreateMockResponse(IApiRequest request)
  {
    var query = (Query)request;

    var responseCreators = new Dictionary<Guid, Func<Response>>
    {
      { MockUserIds.SystemAdmin, CreateMockResponseForAdministrator },
      { MockUserIds.Developer, CreateMockResponseForDeveloper },
    };

    Response response =
      responseCreators.TryGetValue
      (
        query.UserId,
        out Func<Response>? responseCreator
      ) ? responseCreator() : CreateMockResponseForUnknown();

    return response;
  }
  private static Response CreateMockResponseForUnknown()
  {
    return new Response
    (
      modules: AllModules,
      roles:
      [
        RoleIds.Member,
        RoleIds.Administrator,
        RoleIds.Developer
      ]
    );
  }

  private static Response CreateMockResponseForAdministrator()
  {
    return new Response
    (
      modules: AllModules,
      roles:
      [
        RoleIds.Member,
        RoleIds.Administrator,
        RoleIds.Developer
      ]
    );
  }

  private static Response CreateMockResponseForDeveloper()
  {
    return new Response
    (
      modules: AllModules,
      roles: [RoleIds.Member, RoleIds.Developer]
    );
  }
}
