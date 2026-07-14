#region Purpose
// Contract for fetching the signed-in user's module and role grants, with mock responses for backend-less demos.
#endregion

#region Design
// Modules and Roles are Guids drawn from ModuleIds/RoleIds so client menus and route guards key on stable ids
// instead of matching role-name strings.
// The mock factory returns per-user responses keyed by well-known UserIds, letting the SPA demonstrate
// role-driven UI without any auth backend; unknown users get full access because the mock optimizes for
// demo friction, not security.
#endregion

namespace TimeWarp.Architecture.Features.Authentication;

using Authorization;
using TimeWarp.Architecture.Types;
public static partial class GetCurrentUser
{
  [ApiRoute(RouteTemplate: "api/GetCurrentUser", HttpVerb.Get)]
  [ClientOnlyContract("Served by SPA mock mode; the template has no server-side auth slice.")]
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
      { UserIds.SystemAdmin, CreateMockResponseForAdministrator },
      { UserIds.Developer, CreateMockResponseForDeveloper },
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
      roles: [RoleIds.Developer]
    );
  }
}
