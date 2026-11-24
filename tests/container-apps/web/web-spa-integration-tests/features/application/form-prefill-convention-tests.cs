#region Purpose
// Fails when a page-scoped edit-form submit gets a Prefill tool but no form renders FormPrefillScope for it.
#endregion

#region Design
// Task 508. FormPrefill derives the Prefill.<Action> tool from the page registry: an action a
// page lists in [Page(AgentActions = ...)] whose Command implements one I*Details interface. The
// page half is FormPrefillScope<TSubmit> around the form; without it the tool stores a draft no
// form applies. This test walks PageRegistry.ActionScopes for derived submits and requires each
// one to be the type argument of a FormPrefillScope<> that the web-spa assembly references.
// Razor emits `OpenComponent<FormPrefillScope<X>>` for a generic component with an explicit
// type argument, so the closed type shows up in the module's MethodSpec or TypeSpec metadata
// rows; Module.ResolveMethod and Module.ResolveType read them without parsing IL. A row that
// needs a generic context cannot resolve on its own and is skipped; the end of a table is
// ArgumentOutOfRangeException. Unwired is the rule itself, so a fake page scope proves that it
// reports a derived submit with no scope. The expected list pins today's derived submits, so a
// new edit form is a deliberate edit here too.
#endregion

namespace FormPrefillConvention_;

using System.Reflection;
using TimeWarp.Architecture;
using TimeWarp.Architecture.Features.Applications;

[TestTag("Unit")]
public class FormPrefillConvention_Should_
{
  private const int MethodSpecTable = 0x2B000000;
  private const int TypeSpecTable = 0x1B000000;

  private static readonly string[] ExpectedSubmits =
  [
    "AiPage: SiteSettingsState+UpdateAssistantNameActionSet+Action",
    "AiPage: SiteSettingsState+UpdateXaiByokModeActionSet+Action",
    "AuthenticationPage: SiteSettingsState+UpdateSiteSettingsActionSet+Action",
    "ProfilePage: ProfileState+UpdateProfileActionSet+Action",
    "RoleDetailPage: RoleState+UpdateRoleActionSet+Action",
    "RolePage: RoleState+CreateRoleActionSet+Action",
  ];

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FormPrefillConvention_Should_>();

  public static Task Wire_Every_Derived_Edit_Form_Submit()
  {
    HashSet<Type> scoped = ScopedSubmits(typeof(FormPrefillScope<>).Assembly.ManifestModule);
    scoped.ShouldNotBeEmpty();

    List<string> derived = [];
    foreach (PageActionScope page in PageRegistry.ActionScopes)
    {
      foreach (Type action in page.ActionTypes)
      {
        if (FormPrefill.DetailsType(action) is not null)
        {
          derived.Add(page.PageType.Name + ": " + ShortName(action));
        }
      }
    }

    derived.Sort(StringComparer.Ordinal);
    derived.ShouldBe(ExpectedSubmits);
    Unwired(PageRegistry.ActionScopes, scoped).ShouldBeEmpty();
    return Task.CompletedTask;
  }

  public static Task Report_A_Derived_Submit_With_No_Scope()
  {
    PageActionScope fake = new(typeof(FakeEditPage), ["/Fake"], [typeof(FakeSubmit), typeof(FakeNoCommand)]);

    Unwired([fake], []).ShouldBe(["FakeEditPage: FormPrefillConvention_Should_+FakeSubmit"]);
    Unwired([fake], [typeof(FakeSubmit)]).ShouldBeEmpty();
    FormPrefill.DetailsType(typeof(FakeSubmit)).ShouldBe(typeof(IFakeDetails));
    FormPrefill.DetailsType(typeof(FakeNoCommand)).ShouldBeNull();
    return Task.CompletedTask;
  }

  private static List<string> Unwired(IEnumerable<PageActionScope> pages, HashSet<Type> scoped)
  {
    List<string> missing = [];
    foreach (PageActionScope page in pages)
    {
      foreach (Type action in page.ActionTypes)
      {
        if (FormPrefill.DetailsType(action) is not null && !scoped.Contains(action))
        {
          missing.Add(page.PageType.Name + ": " + ShortName(action));
        }
      }
    }

    return missing;
  }

  private static HashSet<Type> ScopedSubmits(Module module)
  {
    HashSet<Type> submits = [];
    for (int row = 1; ; row++)
    {
      MethodBase? method;
      try
      {
        method = module.ResolveMethod(MethodSpecTable | row);
      }
      catch (ArgumentOutOfRangeException)
      {
        break;
      }
      catch (ArgumentException)
      {
        continue;
      }

      if (method is { IsGenericMethod: true })
      {
        foreach (Type argument in method.GetGenericArguments())
        {
          AddIfScope(argument, submits);
        }
      }
    }

    for (int row = 1; ; row++)
    {
      Type type;
      try
      {
        type = module.ResolveType(TypeSpecTable | row);
      }
      catch (ArgumentOutOfRangeException)
      {
        break;
      }
      catch (ArgumentException)
      {
        continue;
      }

      AddIfScope(type, submits);
    }

    return submits;
  }

  private static void AddIfScope(Type type, HashSet<Type> submits)
  {
    if (type.IsGenericType && !type.ContainsGenericParameters
      && type.GetGenericTypeDefinition() == typeof(FormPrefillScope<>))
    {
      submits.Add(type.GetGenericArguments()[0]);
    }
  }

  private static string ShortName(Type type)
  {
    string full = type.FullName ?? type.Name;
    int dot = full.LastIndexOf('.', StringComparison.Ordinal);
    return dot < 0 ? full : full[(dot + 1)..];
  }

  private sealed class FakeEditPage;

  private interface IFakeDetails
  {
    string Name { get; set; }
  }

  private sealed class FakeCommand : IFakeDetails
  {
    public string Name { get; set; } = "";
  }

  private sealed class FakeSubmit
  {
    public FakeCommand Command { get; } = new();
  }

  private sealed class FakeNoCommand
  {
    public string Nickname { get; } = "";
  }
}
