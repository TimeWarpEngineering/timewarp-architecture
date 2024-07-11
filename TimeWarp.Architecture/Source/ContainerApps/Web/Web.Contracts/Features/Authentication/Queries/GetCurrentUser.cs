namespace TimeWarp.Architecture.Features.Authentication;

using Authorization;
using Services;

public static partial class GetCurrentUser
{
  [UsedImplicitly]
  [RouteMixin("api/GetCurrentUser", HttpVerb.Get)]
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
  (
    List<Guid> modules,
    List<Guid> roles
  )
  {
    /// <summary>
    /// List of Module Ids the current user has access to.
    /// </summary>
    /// <remarks> Should be from the ModuleIds</remarks>
    public List<Guid> Modules { get; init; } = modules;


    /// <summary>
    /// List of Roles to which the current user belongs
    /// </summary>
    /// <remarks>Should be from RoleIds</remarks>
    public List<Guid> Roles { get; init; } = roles;
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

  public static MockResponseFactory<Response> CreateMockResponse()
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
