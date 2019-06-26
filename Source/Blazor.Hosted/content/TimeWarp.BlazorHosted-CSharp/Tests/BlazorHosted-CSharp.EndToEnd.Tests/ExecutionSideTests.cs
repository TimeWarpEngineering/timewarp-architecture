namespace BlazorHosted_CSharp.EndToEnd.Tests
{
  using BlazorHosted_CSharp.EndToEnd.Tests.Infrastructure;
  using OpenQA.Selenium;
  using Shouldly;
  using System;
  using System.Threading.Tasks;
  using static Infrastructure.WaitAndAssert;

  public class ExecutionSideTests : BaseTest
  {
    /// <summary>
    ///
    /// </summary>
    /// <param name="aWebDriver"></param>
    /// <param name="aServerFixture">
    /// Is a dependency as the server needs to be running
    /// but is not referenced otherwise thus the injected item is NOT stored
    /// </param>
    public ExecutionSideTests(IWebDriver aWebDriver, ServerFixture aServerFixture)
      : base(aWebDriver, aServerFixture)
    {
      aServerFixture.Environment = AspNetEnvironment.Development;
      aServerFixture.CreateHostBuilderDelegate = Server.Program.CreateHostBuilder;

      Navigate("/", aReload: true);
      WaitUntilLoaded();
    }

    public void LoadsServerSide()
    {
      JavaScriptExecutor.ExecuteScript("window.localStorage.setItem('executionSide','server');");

      Navigate("/", aReload: true);
      WaitUntilLoaded();

      WaitAndAssertEqual(
        aExpected: "Server Side",
        aActual: () => WebDriver.FindElement(By.CssSelector("[data-qa='BlazorLocation']")).Text
      );
    }

    public void LoadsClientSide()
    {
      WaitUntilClientCached();
      JavaScriptExecutor.ExecuteScript("window.localStorage.setItem('executionSide','client');");

      Navigate("/", aReload: true);
      WaitUntilClientCached();

      WaitAndAssertEqual(
        aExpected: "Client Side",
        aActual: () => WebDriver.FindElement(By.CssSelector("[data-qa='BlazorLocation']")).Text
      );
    }
  }
}