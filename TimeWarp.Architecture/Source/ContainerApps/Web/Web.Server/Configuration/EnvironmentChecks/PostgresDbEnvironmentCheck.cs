// TOOD: This is copilot generated code, it needs to be reviewed and cleaned up
namespace TimeWarp.Architecture.Configuration;

public class PostgresDbEnvironmentCheck
{
  private readonly PostgresDbOptions PostgresDbOptions;
  private readonly IServiceProvider ServiceProvider;
  private readonly ILogger Logger;

  public PostgresDbEnvironmentCheck
  (
      IOptions<PostgresDbOptions> postgresDbOptionsAccessor,
      IServiceProvider serviceProvider,
      ILogger<PostgresDbEnvironmentCheck> logger
  )
  {
    PostgresDbOptions = postgresDbOptionsAccessor.Value;
    ServiceProvider = serviceProvider;
    Logger = logger;
  }

  public static string Description => "Connecting to PostgreSQL";

  public async Task<bool> CheckAsync()
  {
    Logger.LogInformation($"Start {nameof(PostgresDbEnvironmentCheck)} ");

    using IServiceScope scope = ServiceProvider.CreateScope();

    PostgresDbContext postgresDbContext = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();

    try
    {
      await postgresDbContext.Database.CanConnectAsync().ConfigureAwait(true);
    }
    catch (HttpRequestException)
    {
      return false;
    }

    Logger.LogInformation($"Completed {nameof(PostgresDbEnvironmentCheck)} ");
    return true;
  }
}

