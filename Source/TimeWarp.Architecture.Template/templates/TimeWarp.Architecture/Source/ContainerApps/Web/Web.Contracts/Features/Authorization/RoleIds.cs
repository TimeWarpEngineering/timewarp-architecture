namespace TimeWarp.Architecture.Features.Authorization;

public static class RoleIds
{
  [UsedImplicitly] public static readonly Guid Administrator = new Guid("834B9073-D5FF-40B3-938A-968C23FA76CC");
  [UsedImplicitly] public static readonly Guid Accountant = new Guid("290A5645-4913-4845-BDC0-4C7B11EE5E83");
  [UsedImplicitly] public static readonly Guid AccountsPayableClerk = new Guid("44EC68B4-86F9-4ED4-BE44-609C9345146F");
  [UsedImplicitly] public static readonly Guid AccountsReceivableClerk = new Guid("0FA1340D-5324-4DDC-AB8A-9F6BB9AFC266");
  [UsedImplicitly] public static readonly Guid Auditor = new Guid("9427FCB5-8559-485D-A401-4CC96AFAC2ED");
  [UsedImplicitly] public static readonly Guid FinancialAnalyst = new Guid("AAD06C5D-0AD2-4D40-8E67-BFC0D4DB627E");
  [UsedImplicitly] public static readonly Guid ChiefFinancialOfficer = new Guid("EC5AA183-C983-492F-BC62-88DEB0D0ADA0");
  [UsedImplicitly] public static readonly Guid Controller = new Guid("68ECDAD3-C4C9-4E8C-A70A-500C5376627C");
  [UsedImplicitly] public static readonly Guid PurchasingManager = new Guid("1398BD91-B01C-4BFB-AF0B-B143A61B90B4");
  [UsedImplicitly] public static readonly Guid InventoryManager = new Guid("F5181CC9-0A72-4E49-A625-60625F935045");
  [UsedImplicitly] public static readonly Guid PayrollManager = new Guid("D4F24977-6C32-4491-BBDE-75F559030DDF");
  [UsedImplicitly] public static readonly Guid SalesManager = new Guid("9AC229C2-36A2-45F1-98A6-69B91AE87AC7");
  [UsedImplicitly] public static readonly Guid ProjectManager = new Guid("6D5A0EA2-3F1A-4B0D-93E6-6380214F207A");
  [UsedImplicitly] public static readonly Guid ComplianceOfficer = new Guid("2C677132-2FC7-4401-8A4D-97A26BAE49D3");
  [UsedImplicitly] public static readonly Guid TaxSpecialist = new Guid("098C7CEC-FE2E-4B12-B699-0031418E47CB");

  [UsedImplicitly] public static readonly Guid Developer = new Guid("80EE3E0C-A8B6-45D6-BA27-7DEE2691AA42");

  public static string GetRoleNameByGuid(Guid roleId)
  {
    // Retrieve all static public fields of the RoleIds class
    FieldInfo[] roleFields = typeof(RoleIds).GetFields(BindingFlags.Static | BindingFlags.Public);

    foreach (FieldInfo field in roleFields)
    {
      // Ensure the field is of type Guid before proceeding
      if (field.FieldType != typeof(Guid)) continue;

      var fieldValue = (Guid)(field.GetValue(null) ?? Guid.Empty);// Safe cast as we already checked the type

      // Check if the current field's value matches the provided roleId
      if (fieldValue == roleId) return field.Name;// Return the matching field's name
    }

    // Return a default value if no matching field was found
    return "Unknown Role";
  }
}
