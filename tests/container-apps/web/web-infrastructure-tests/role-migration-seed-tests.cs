// The AddRoles migration seeds the four product roles with Guid literals (task 473). These tests
// read the migration's own Up operations, so they pin what the migration does without a database.

namespace Role_Migration_Seed_;

using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Admin.Roles.Application;
using TimeWarp.Architecture.Persistence.Migrations;

public class AddRoles
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AddRoles>();

  public static Task Seeded_ids_equal_RoleIds_All()
  {
    SeededRows().Select(row => row.Id).ShouldBe(RoleIds.All);
    return Task.CompletedTask;
  }

  public static Task Seeded_names_and_descriptions_equal_ProductRoleSeed()
  {
    SeededRows().ShouldBe(ProductRoleSeed.Roles.Select(role => (role.Id, role.Name, role.Description)));
    return Task.CompletedTask;
  }

  public static Task Parents_exist_before_the_foreign_keys_are_added()
  {
    IReadOnlyList<MigrationOperation> operations = UpOperations();
    int seed = IndexOf(operations, operation => operation is InsertDataOperation { Table: "roles" });
    int strayParents = IndexOf(operations, operation => operation is SqlOperation);
    int firstForeignKey = IndexOf(operations, operation => operation is AddForeignKeyOperation);

    seed.ShouldBeGreaterThanOrEqualTo(0);
    strayParents.ShouldBeGreaterThan(seed);
    firstForeignKey.ShouldBeGreaterThan(strayParents);
    operations.OfType<AddForeignKeyOperation>().Select(operation => operation.Table).Order()
      .ShouldBe(["role_agent_scopes", "role_permissions"]);
    return Task.CompletedTask;
  }

  private static List<(Guid Id, string Name, string Description)> SeededRows()
  {
    InsertDataOperation insert = UpOperations().OfType<InsertDataOperation>().Single(operation => operation.Table == "roles");
    int id = Array.IndexOf(insert.Columns, "Id");
    int name = Array.IndexOf(insert.Columns, "Name");
    int description = Array.IndexOf(insert.Columns, "Description");
    return
    [
      .. Enumerable.Range(0, insert.Values.GetLength(0))
        .Select(row => ((Guid)insert.Values[row, id]!, (string)insert.Values[row, name]!, (string)insert.Values[row, description]!))
    ];
  }

  private static IReadOnlyList<MigrationOperation> UpOperations() =>
    new _20261104193441_AddRoles { ActiveProvider = "Npgsql.EntityFrameworkCore.PostgreSQL" }.UpOperations;

  private static int IndexOf(IReadOnlyList<MigrationOperation> operations, Func<MigrationOperation, bool> match)
  {
    for (int index = 0; index < operations.Count; index++)
    {
      if (match(operations[index]))
      {
        return index;
      }
    }

    return -1;
  }
}
