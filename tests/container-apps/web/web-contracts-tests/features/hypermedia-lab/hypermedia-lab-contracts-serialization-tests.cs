#region Purpose
// Round-trip tests for the hypermedia-lab contracts (task 275): GetCredentialOffers (approach B) and
// GetCredentialCommands (approach C) Query and Response, including the JsonElement argument/body
// shapes and the generated route members.
#endregion

#region Design
// Both Responses carry ctor-Guarded read-only lists and dictionaries of JsonElement, which is where
// serialization can diverge (ctor binding, JsonElement surviving a round-trip as the same JSON text).
// Mock factory output is used as the realistic fixture; hand-built shapes cover the optional fields
// (null Subject, null Body, empty Arguments) that the mock does not exercise. Routes are pinned as
// literals because the SPA client and the server both derive them from the generated members.
#endregion

// ReSharper disable InconsistentNaming
namespace HypermediaLabContracts_;

using TimeWarp.Architecture.Features.HypermediaLab;
using TimeWarp.Architecture.Web.Contracts.Tests;

public class GetCredentialOffers_Query_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<GetCredentialOffers_Query_Should>();

  public static Task SerializeAndDeserialize()
  {
    GetCredentialOffers.Query parsed = ContractSerialization.RoundTrip(new GetCredentialOffers.Query());

    parsed.ShouldNotBeNull();
    return Task.CompletedTask;
  }

  public static Task Expose_Generated_Route()
  {
    new GetCredentialOffers.Query().GetRoute().ShouldBe("api/hypermedia-lab/credential-offers");
    return Task.CompletedTask;
  }
}

public class GetCredentialOffers_Response_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<GetCredentialOffers_Response_Should>();

  public static Task SerializeAndDeserialize_Mock_Response()
  {
    GetCredentialOffers.Response response = GetCredentialOffers.GetMockResponseFactory()(new GetCredentialOffers.Query());

    GetCredentialOffers.Response parsed = ContractSerialization.RoundTrip(response);

    parsed.Credentials.Count.ShouldBe(response.Credentials.Count);
    parsed.Credentials[0].Id.ShouldBe(response.Credentials[0].Id);
    parsed.Offers.Count.ShouldBe(response.Offers.Count);
    for (int index = 0; index < response.Offers.Count; index++)
    {
      parsed.Offers[index].Name.ShouldBe(response.Offers[index].Name);
      parsed.Offers[index].Label.ShouldBe(response.Offers[index].Label);
      parsed.Offers[index].Subject.ShouldBe(response.Offers[index].Subject);
      parsed.Offers[index].Arguments[GetCredentialOffers.OfferedActionNames.CredentialIdArgument].GetString()
        .ShouldBe(response.Offers[index].Subject);
    }

    return Task.CompletedTask;
  }

  public static Task SerializeAndDeserialize_PageLevel_Offer_With_Empty_Arguments()
  {
    GetCredentialOffers.Response response = new
    (
      [],
      [new GetCredentialOffers.OfferedAction(GetCredentialOffers.OfferedActionNames.LinkMicrosoft365, "Link Microsoft 365", subject: null, new Dictionary<string, JsonElement>())]
    );

    GetCredentialOffers.Response parsed = ContractSerialization.RoundTrip(response);

    GetCredentialOffers.OfferedAction offer = parsed.Offers.Single();
    offer.Name.ShouldBe(GetCredentialOffers.OfferedActionNames.LinkMicrosoft365);
    offer.Subject.ShouldBeNull();
    offer.Arguments.ShouldBeEmpty();
    return Task.CompletedTask;
  }

  public static Task Reject_Missing_Offers_During_Deserialization()
  {
    const string json = """{"credentials":[]}""";

    Should.Throw<Exception>(() =>
      JsonSerializer.Deserialize<GetCredentialOffers.Response>(json, ContractSerialization.Options));
    return Task.CompletedTask;
  }
}

public class GetCredentialCommands_Query_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<GetCredentialCommands_Query_Should>();

  public static Task SerializeAndDeserialize()
  {
    GetCredentialCommands.Query parsed = ContractSerialization.RoundTrip(new GetCredentialCommands.Query());

    parsed.ShouldNotBeNull();
    return Task.CompletedTask;
  }

  public static Task Expose_Generated_Route()
  {
    new GetCredentialCommands.Query().GetRoute().ShouldBe("api/hypermedia-lab/credential-commands");
    GetCredentialCommands.SelfHref.ShouldBe("/api/hypermedia-lab/credential-commands");
    return Task.CompletedTask;
  }
}

public class GetCredentialCommands_Response_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<GetCredentialCommands_Response_Should>();

  public static Task SerializeAndDeserialize_Mock_Response()
  {
    GetCredentialCommands.Response response = GetCredentialCommands.GetMockResponseFactory()(new GetCredentialCommands.Query());

    GetCredentialCommands.Response parsed = ContractSerialization.RoundTrip(response);

    parsed.Self.ShouldBe(response.Self);
    parsed.Credentials.Count.ShouldBe(response.Credentials.Count);
    parsed.Commands.Count.ShouldBe(response.Commands.Count);
    for (int index = 0; index < response.Commands.Count; index++)
    {
      GetCredentialCommands.LinkCommand expected = response.Commands[index];
      GetCredentialCommands.LinkCommand actual = parsed.Commands[index];
      actual.Rel.ShouldBe(expected.Rel);
      actual.Label.ShouldBe(expected.Label);
      actual.Subject.ShouldBe(expected.Subject);
      actual.Method.ShouldBe(expected.Method);
      actual.Href.ShouldBe(expected.Href);
      actual.Fields.ShouldBe(expected.Fields);
      actual.Body.ShouldNotBeNull();
      actual.Body[GetCredentialCommands.LinkCommandRels.UserIdField].GetGuid()
        .ShouldBe(expected.Body![GetCredentialCommands.LinkCommandRels.UserIdField].GetGuid());
    }

    return Task.CompletedTask;
  }

  public static Task Carry_Real_Routes_As_App_Relative_Hrefs()
  {
    GetCredentialCommands.Response response = GetCredentialCommands.GetMockResponseFactory()(new GetCredentialCommands.Query());

    GetCredentialCommands.LinkCommand revoke = response.Commands.First(c => c.Rel == GetCredentialCommands.LinkCommandRels.Revoke);
    GetCredentialCommands.LinkCommand rename = response.Commands.First(c => c.Rel == GetCredentialCommands.LinkCommandRels.Rename);

    revoke.Href.ShouldBe($"/api/identity/credentials/{revoke.Subject}/revoke");
    rename.Href.ShouldBe($"/api/identity/credentials/{rename.Subject}/rename");
    rename.Fields.ShouldBe([GetCredentialCommands.LinkCommandRels.NicknameField]);
    response.Commands.ShouldAllBe(c => c.Href.StartsWith('/') && !c.Href.StartsWith("//"));
    return Task.CompletedTask;
  }

  public static Task SerializeAndDeserialize_Navigate_Command_Without_Body()
  {
    GetCredentialCommands.LinkCommand navigate = GetCredentialCommands.LinkMicrosoft365("/HypermediaLab");
    GetCredentialCommands.Response response = new([], [navigate], GetCredentialCommands.SelfHref);

    GetCredentialCommands.Response parsed = ContractSerialization.RoundTrip(response);

    GetCredentialCommands.LinkCommand command = parsed.Commands.Single();
    command.Method.ShouldBe(GetCredentialCommands.LinkCommandMethods.Navigate);
    command.Body.ShouldBeNull();
    command.Subject.ShouldBeNull();
    command.Href.ShouldBe(navigate.Href);
    return Task.CompletedTask;
  }

  public static Task Reject_Blank_Self_During_Deserialization()
  {
    const string json = """{"credentials":[],"commands":[],"self":" "}""";

    Should.Throw<Exception>(() =>
      JsonSerializer.Deserialize<GetCredentialCommands.Response>(json, ContractSerialization.Options));
    return Task.CompletedTask;
  }
}
