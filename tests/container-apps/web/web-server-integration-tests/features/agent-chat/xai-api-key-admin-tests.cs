#region Purpose
// HTTP proof that an admin key is write-only, encrypted, used by Ask, and refused to a non-admin.
#endregion

#region Design
// One database, two web-server hosts. The first host sets and replaces the key over HTTP. Ask's
// resolver on that same host returns the new key. The second host unprotects it, so the Data
// Protection ring in Postgres is what was written. A host XAI:ApiKey on a third host wins and
// rejects writes. Template-excluded when postgres is off.
#endregion

namespace XaiApiKeyAdmin_;

using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.AgentChats;
using TimeWarp.Architecture.Features.AgentChats.Application;
using GetXaiApiKeyStatus = TimeWarp.Architecture.Features.AgentChats.GetXaiApiKeyStatus;
using TimeWarp.Architecture.Persistence;
using TimeWarp.Architecture.Secrets;
using TimeWarp.Identity;
using static TimeWarp.Architecture.Web.Server.Integration.Tests.Features.Identity.Infrastructure.CredentialCeremonyHelpers;

public class Returns_
{
  private const string Secret = "xai-test-key-398-aaaa";
  private const string Replacement = "xai-test-key-398-bbbb";
  private const string HostSecret = "xai-host-key-398-hhhh";

  private static string? ConnectionString;
  private static bool Skip;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Returns_>();

  public static async Task SetupOnce()
  {
    PostgresTestAvailability availability = await PostgresTestAvailability.ResolveAsync("timewarp_xai_api_key_admin_tests");
    if (availability.AdminConnectionString is not { } adminConnectionString)
    {
      if (PostgresTestAvailability.IsCiEnvironment())
      {
        throw new InvalidOperationException(
          "Admin xAI key regression requires Postgres under CI. " + availability.SkipReason);
      }

      Skip = true;
      Console.WriteLine($"[SKIP] XaiApiKeyAdmin_: {availability.SkipReason ?? "no connection"}");
      return;
    }

    ConnectionString = await CreateDatabaseAsync(adminConnectionString);
    await using PostgresDbContext db = new(
      new DbContextOptionsBuilder<PostgresDbContext>().UseNpgsql(ConnectionString).Options);
    await db.Database.MigrateAsync();
  }

  public static Task CleanUpOnce() => Task.CompletedTask;

  public static async Task Stored_Key_Should_Be_Write_Only_Encrypted_And_Used_By_Ask()
  {
    if (Skip)
    {
      return;
    }

    await using (HostGraph first = await StartAsync(apiKey: ""))
    {
    using HttpClient anonymous = Client(first);
    (await anonymous.GetAsync("api/admin/ai/xai-api-key")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

    (PrincipalId memberId, string memberCookie) = await RegisterPasskeyAndMintSessionAsync(first.Web!);
    await SetRolesAsync(first, memberId, [RoleIds.Member]);
    using HttpClient member = Client(first, memberCookie);
    (await member.GetAsync("api/admin/ai/xai-api-key")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    (await member.PutAsync("api/admin/ai/xai-api-key", Json(Secret))).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    (await member.DeleteAsync("api/admin/ai/xai-api-key")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

    (PrincipalId adminId, string adminCookie) = await RegisterPasskeyAndMintSessionAsync(first.Web!);
    await SetRolesAsync(first, adminId, [RoleIds.Member, RoleIds.Administrator]);
    using HttpClient admin = Client(first, adminCookie);

    HttpResponseMessage set = await admin.PutAsync("api/admin/ai/xai-api-key", Json(Secret));
    set.StatusCode.ShouldBe(HttpStatusCode.OK);
    string setJson = await set.Content.ReadAsStringAsync();
    setJson.ShouldNotContain(Secret);
    PropertyNames(setJson).ShouldNotContain("apiKey");
    GetXaiApiKeyStatus.Response created = Read(setJson);
    created.Configured.ShouldBeTrue();
    created.ManagedByHost.ShouldBeFalse();
    created.CanChange.ShouldBeTrue();
    created.Suffix.ShouldBe("aaaa");
    created.SetByPrincipalId.ShouldBe(adminId.Value);

    (await ResolveAsync(first)).ShouldBe(Secret);

    HttpResponseMessage replaced = await admin.PutAsync("api/admin/ai/xai-api-key", Json(Replacement));
    replaced.StatusCode.ShouldBe(HttpStatusCode.OK);
    string replacedJson = await replaced.Content.ReadAsStringAsync();
    replacedJson.ShouldNotContain(Replacement);
    Read(replacedJson).Suffix.ShouldBe("bbbb");
    (await ResolveAsync(first)).ShouldBe(Replacement);

    await using (AsyncServiceScope scope = first.Web!.WebApplicationHost.ServiceProvider.CreateAsyncScope())
    {
      PostgresDbContext db = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();
      ProtectedSecret row = await db.ProtectedSecrets.SingleAsync();
      row.Name.ShouldBe(SiteXaiApiKeyNames.Name);
      row.Purpose.ShouldBe(SiteXaiApiKeyNames.Purpose);
      row.Ciphertext.ShouldNotContain(Replacement);
      row.Ciphertext.ShouldNotContain(Secret);
      row.Suffix.ShouldBe("bbbb");
      string plain = scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>()
        .CreateProtector(SiteXaiApiKeyNames.Purpose)
        .Unprotect(row.Ciphertext);
      plain.ShouldBe(Replacement);
    }
    }

    await using (HostGraph second = await StartAsync(apiKey: ""))
    {
      (await ResolveAsync(second)).ShouldBe(Replacement);
    }

    await using (HostGraph hostOwned = await StartAsync(apiKey: HostSecret))
    {
      (await ResolveAsync(hostOwned)).ShouldBe(HostSecret);
      (PrincipalId hostAdmin, string hostCookie) = await RegisterPasskeyAndMintSessionAsync(hostOwned.Web!);
      await SetRolesAsync(hostOwned, hostAdmin, [RoleIds.Member, RoleIds.Administrator]);
      using HttpClient hostClient = Client(hostOwned, hostCookie);
      HttpResponseMessage hostStatus = await hostClient.GetAsync("api/admin/ai/xai-api-key");
      hostStatus.StatusCode.ShouldBe(HttpStatusCode.OK);
      string hostJson = await hostStatus.Content.ReadAsStringAsync();
      hostJson.ShouldNotContain(HostSecret);
      hostJson.ShouldNotContain(Replacement);
      GetXaiApiKeyStatus.Response hostView = Read(hostJson);
      hostView.ManagedByHost.ShouldBeTrue();
      hostView.CanChange.ShouldBeFalse();
      hostView.Suffix.ShouldBeNull();
      HttpResponseMessage refused = await hostClient.PutAsync("api/admin/ai/xai-api-key", Json(Secret));
      refused.StatusCode.ShouldBe(HttpStatusCode.Conflict);
      (await refused.Content.ReadAsStringAsync()).ShouldNotContain(Secret);
    }

    await using (HostGraph clearing = await StartAsync(apiKey: ""))
    {
      (PrincipalId clearAdmin, string clearCookie) = await RegisterPasskeyAndMintSessionAsync(clearing.Web!);
      await SetRolesAsync(clearing, clearAdmin, [RoleIds.Member, RoleIds.Administrator]);
      using HttpClient clearClient = Client(clearing, clearCookie);
      HttpResponseMessage cleared = await clearClient.DeleteAsync("api/admin/ai/xai-api-key");
      cleared.StatusCode.ShouldBe(HttpStatusCode.OK);
      string clearedJson = await cleared.Content.ReadAsStringAsync();
      clearedJson.ShouldNotContain(Replacement);
      Read(clearedJson).Configured.ShouldBeFalse();
      (await ResolveAsync(clearing)).ShouldBeNull();
    }
  }

  private static async Task<HostGraph> StartAsync(string apiKey)
  {
    Dictionary<string, string?> configuration = new(WebTestServerApplication.HttpsServiceEndpoints)
    {
      ["PostgresDbOptions:ConnectionString"] = ConnectionString,
      ["XAI:ApiKey"] = apiKey,
      ["XAI:UseFakeUpstream"] = "false",
    };
    return await HostGraphFactory.CreateWebAsync(serviceEndpoints: configuration);
  }

  private static async Task<string?> ResolveAsync(HostGraph graph)
  {
    await using AsyncServiceScope scope = graph.Web!.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    ISiteXaiApiKey keys = scope.ServiceProvider.GetRequiredService<ISiteXaiApiKey>();
    return await keys.ResolveApiKeyAsync(CancellationToken.None);
  }

  private static async Task SetRolesAsync(HostGraph graph, PrincipalId principalId, Guid[] roles)
  {
    await using AsyncServiceScope scope = graph.Web!.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IPrincipalRoleStore roleStore = scope.ServiceProvider.GetRequiredService<IPrincipalRoleStore>();
    await roleStore.SetRoleIdsAsync(principalId, roles);
  }

  private static HttpClient Client(HostGraph graph, string? cookie = null)
  {
    HttpClient client = new() { BaseAddress = graph.Web!.HttpClient.BaseAddress };
    if (cookie is not null)
    {
      client.DefaultRequestHeaders.Add("Cookie", cookie);
    }

    return client;
  }

  private static StringContent Json(string apiKey) =>
    new(JsonSerializer.Serialize(new { apiKey }, ContractSerializationDefaults.Options), Encoding.UTF8, "application/json");

  private static string[] PropertyNames(string json)
  {
    using JsonDocument document = JsonDocument.Parse(json);
    return [.. document.RootElement.EnumerateObject().Select(static property => property.Name)];
  }

  private static GetXaiApiKeyStatus.Response Read(string json) =>
    JsonSerializer.Deserialize<GetXaiApiKeyStatus.Response>(json, ContractSerializationDefaults.Options)
    ?? throw new InvalidOperationException("XAI API key status deserialized to null.");

  private static async Task<string> CreateDatabaseAsync(string adminConnectionString)
  {
    string databaseName = "xai_" + Guid.NewGuid().ToString("N");
    await using NpgsqlConnection connection = new(adminConnectionString);
    await connection.OpenAsync();
    await using NpgsqlCommand command = connection.CreateCommand();
#pragma warning disable CA2100 // databaseName is minted above from a Guid, never caller input.
    command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
#pragma warning restore CA2100
    await command.ExecuteNonQueryAsync();
    return new NpgsqlConnectionStringBuilder(adminConnectionString) { Database = databaseName }.ConnectionString;
  }
}
