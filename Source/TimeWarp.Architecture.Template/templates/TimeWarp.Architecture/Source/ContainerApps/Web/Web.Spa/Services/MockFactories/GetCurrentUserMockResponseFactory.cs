namespace TimeWarp.Architecture.Services;

internal sealed class GetCurrentUserMockResponseFactory : IMockResponseFactory
{
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
  public object CreateMockResponse(dynamic request)
  {
    GetCurrentUser.Query query = request;

    var responseCreators = new Dictionary<Guid, Func<GetCurrentUser.Response>>
    {
      { UserIds.SystemAdmin, CreateMockResponseForAdministrator },
      { UserIds.Developer, CreateMockResponseForDeveloper },
    };

    GetCurrentUser.Response response =
      responseCreators.TryGetValue(query.UserId, out Func<GetCurrentUser.Response>? responseCreator) ?
        responseCreator() :
        CreateMockResponseForUnknown();

    // response.Roles?.Add(RoleIds.Developer);

    return response;
  }
  private static GetCurrentUser.Response CreateMockResponseForUnknown()
  {
    return new GetCurrentUser.Response
    (
      modules: AllModules,
      roles:
      [
        RoleIds.Administrator,
        RoleIds.Developer
      ]
    );
  }

  private static GetCurrentUser.Response CreateMockResponseForAdministrator()
  {
    return new GetCurrentUser.Response
    (
      modules: AllModules,
      roles:
      [
        RoleIds.Administrator,
        RoleIds.Developer
      ]
    );
  }

  private static GetCurrentUser.Response CreateMockResponseForDeveloper()
  {
    return new GetCurrentUser.Response
    (
      modules: AllModules,
      roles: [RoleIds.Developer]
    );
  }
}
