#region Purpose
// Ensures Postgres schema matches the current EF model on every host start (greenfield + evolve).
#endregion

#region Design
// EnsureCreatedAsync alone is wrong for a living template: it is a no-op when *any* tables already
// exist, so adding an entity (e.g. identity.principal_roles) never materializes on Aspire data
// volumes. This bootstrap:
//   1) EnsureCreatedAsync — creates the full model when the database is empty
//   2) If the DB already had tables, diffs empty → current model via IMigrationsModelDiffer and
//      executes only EnsureSchema / CreateTable / CreateIndex / AddForeignKey for objects that
//      are still missing (CREATE-style ops only; no destructive changes).
// Grown apps that need renames/column changes should switch to Database.Migrate (ADR-0009); this
// path is template dogfood automation so operators never hand-run DDL for new tables.
#endregion

namespace TimeWarp.Architecture.HostedServices;

using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using TimeWarp.Architecture.Persistence;

/// <summary>Applies missing model tables/schemas at startup without full EF migrations.</summary>
public static class PostgresModelSchemaBootstrap
{
  public static async Task EnsureSchemaMatchesModelAsync(
    PostgresDbContext dbContext,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(dbContext);

    bool created = await dbContext.Database
      .EnsureCreatedAsync(cancellationToken)
      .ConfigureAwait(false);

    if (created)
    {
      return;
    }

    await CreateMissingRelationalObjectsAsync(dbContext, cancellationToken).ConfigureAwait(false);
  }

  private static async Task CreateMissingRelationalObjectsAsync(
    PostgresDbContext dbContext,
    CancellationToken cancellationToken)
  {
    IMigrationsModelDiffer differ = dbContext.GetService<IMigrationsModelDiffer>();
    IMigrationsSqlGenerator sqlGenerator = dbContext.GetService<IMigrationsSqlGenerator>();
    IMigrationCommandExecutor commandExecutor = dbContext.GetService<IMigrationCommandExecutor>();
    IRelationalConnection connection = dbContext.GetService<IRelationalConnection>();

    IRelationalModel targetModel = dbContext.Model.GetRelationalModel();
    IReadOnlyList<MigrationOperation> allOperations = differ.GetDifferences(
      source: null,
      target: targetModel);

    HashSet<(string Schema, string Table)> missingTables = new();
    List<MigrationOperation> pending = [];

    foreach (MigrationOperation operation in allOperations)
    {
      switch (operation)
      {
        case EnsureSchemaOperation ensureSchema:
          if (!await SchemaExistsAsync(dbContext, ensureSchema.Name, cancellationToken)
                .ConfigureAwait(false))
          {
            pending.Add(operation);
          }

          break;

        case CreateTableOperation createTable:
        {
          string schema = createTable.Schema ?? "public";
          if (!await TableExistsAsync(dbContext, schema, createTable.Name, cancellationToken)
                .ConfigureAwait(false))
          {
            pending.Add(operation);
            missingTables.Add((schema, createTable.Name));
          }

          break;
        }
      }
    }

    if (missingTables.Count == 0)
    {
      return;
    }

    // Indexes / FKs for tables we are about to create (ops for already-present tables stay skipped).
    foreach (MigrationOperation operation in allOperations)
    {
      switch (operation)
      {
        case CreateIndexOperation indexOp:
        {
          string schema = indexOp.Schema ?? "public";
          if (missingTables.Contains((schema, indexOp.Table)))
          {
            pending.Add(operation);
          }

          break;
        }
        case AddForeignKeyOperation fkOp:
        {
          string schema = fkOp.Schema ?? "public";
          if (missingTables.Contains((schema, fkOp.Table)))
          {
            pending.Add(operation);
          }

          break;
        }
      }
    }

    if (pending.Count == 0)
    {
      return;
    }

    IReadOnlyList<MigrationCommand> commands = sqlGenerator.Generate(pending, dbContext.Model);
    await commandExecutor
      .ExecuteNonQueryAsync(commands, connection, cancellationToken)
      .ConfigureAwait(false);
  }

  private static async Task<bool> SchemaExistsAsync(
    DbContext dbContext,
    string schema,
    CancellationToken cancellationToken)
  {
    await dbContext.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
    try
    {
      DbConnection connection = dbContext.Database.GetDbConnection();
      await using DbCommand command = connection.CreateCommand();
      command.CommandText =
        """
        SELECT EXISTS (
          SELECT 1 FROM information_schema.schemata WHERE schema_name = @schema
        );
        """;
      DbParameter parameter = command.CreateParameter();
      parameter.ParameterName = "schema";
      parameter.Value = schema;
      command.Parameters.Add(parameter);

      object? result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
      return result is true;
    }
    finally
    {
      await dbContext.Database.CloseConnectionAsync().ConfigureAwait(false);
    }
  }

  private static async Task<bool> TableExistsAsync(
    DbContext dbContext,
    string schema,
    string table,
    CancellationToken cancellationToken)
  {
    await dbContext.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
    try
    {
      DbConnection connection = dbContext.Database.GetDbConnection();
      await using DbCommand command = connection.CreateCommand();
      command.CommandText =
        """
        SELECT EXISTS (
          SELECT 1
          FROM information_schema.tables
          WHERE table_schema = @schema AND table_name = @table
        );
        """;
      DbParameter schemaParameter = command.CreateParameter();
      schemaParameter.ParameterName = "schema";
      schemaParameter.Value = schema;
      command.Parameters.Add(schemaParameter);

      DbParameter tableParameter = command.CreateParameter();
      tableParameter.ParameterName = "table";
      tableParameter.Value = table;
      command.Parameters.Add(tableParameter);

      object? result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
      return result is true;
    }
    finally
    {
      await dbContext.Database.CloseConnectionAsync().ConfigureAwait(false);
    }
  }
}
