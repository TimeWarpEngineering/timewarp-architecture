#region Purpose
// Layout assertions for the Ask surface Playwright proof: send button, composer, @ picker, docked panel
// geometry and icon buttons, plus the screenshot path shared by the Ask surface helpers.
#endregion

namespace AskSurfacePlaywright_;

using static AskSurfacePlaywright_.AskSurfaceInteractionAsserts;

internal static class AskSurfaceLayoutAsserts
{
  internal static string ScreenshotPath(string fileName) =>
    PlaywrightScreenshot.PathFor<AskSurface_Given_Wasm>(fileName);

  internal static async Task AssertSendMutedThenSolidAsync(IPage page, ILocator input, string prompt)
  {
    const string sendSelector = ".twe-agent-ask .sc-ai-input__send";
    ILocator send = page.Locator(sendSelector);
    (await send.GetAttributeAsync("aria-label")).ShouldBe("Send message");
    (await send.Locator("svg").EvaluateAsync<string>("svg => getComputedStyle(svg).visibility")).ShouldBe("hidden");
    await page.Locator(".twe-agent-ask [data-qa=AskSendGlyph]").WaitForAsync();
    LocatorBoundingBoxResult sendBox = await BoxAsync(page, sendSelector);
    LocatorBoundingBoxResult glyphBox = await BoxAsync(page, ".twe-agent-ask [data-qa=AskSendGlyph]");
    Math.Abs(glyphBox.X - sendBox.X).ShouldBeLessThanOrEqualTo(1);
    Math.Abs(glyphBox.Y - sendBox.Y).ShouldBeLessThanOrEqualTo(1);
    (await input.InputValueAsync()).ShouldBeEmpty();

    // The background transitions, so wait for the end color rather than reading it once.
    const string isPrimary = """
      ([selector, wanted]) => {
        const send = document.querySelector(selector);
        const probe = document.createElement('span');
        probe.style.color = 'var(--twe-primary)';
        send.parentElement.appendChild(probe);
        const primary = getComputedStyle(probe).color;
        probe.remove();
        return (getComputedStyle(send).backgroundColor === primary) === wanted;
      }
      """;
    await page.WaitForFunctionAsync(isPrimary, new object[] { sendSelector, false });
    await page.Locator(".twe-agent-ask").ScreenshotAsync(new LocatorScreenshotOptions
    {
      Path = ScreenshotPath("ask-send-muted.png"),
    });
    await input.FillAsync(prompt);
    await page.WaitForFunctionAsync(isPrimary, new object[] { sendSelector, true });
    (await send.IsEnabledAsync()).ShouldBeTrue();
    await page.Locator(".twe-agent-ask .sc-ai-input").ScreenshotAsync(new LocatorScreenshotOptions
    {
      Path = ScreenshotPath("ask-send-solid.png"),
    });
  }

  internal static async Task AssertComposerLayoutAsync(IPage page)
  {
    const string areaSelector = "textarea.sc-ai-input__textarea";
    const string composerSelector = ".twe-agent-ask .sc-ai-input";
    ILocator textarea = page.Locator(areaSelector);
    ILocator composer = page.Locator(composerSelector);
    string? placeholder = await textarea.GetAttributeAsync("placeholder");
    placeholder.ShouldNotBeNull();
    placeholder.ShouldContain("Type @ to reference a resource");
    (await page.Locator("[data-qa=AskTagResource]").CountAsync()).ShouldBe(0);

    LocatorBoundingBoxResult composerBox = await BoxAsync(page, composerSelector);
    LocatorBoundingBoxResult areaBox = await BoxAsync(page, areaSelector);
    LocatorBoundingBoxResult sendBox = await BoxAsync(page, ".twe-agent-ask .sc-ai-input__send");
    double inset = areaBox.X - composerBox.X;
    inset.ShouldBeGreaterThanOrEqualTo(4);
    inset.ShouldBeLessThanOrEqualTo(20);
    ((double)areaBox.Width).ShouldBeGreaterThanOrEqualTo(composerBox.Width * 0.9);
    ((double)sendBox.Y).ShouldBeGreaterThanOrEqualTo(areaBox.Y + areaBox.Height - 2);
    double sendInset = composerBox.X + composerBox.Width - (sendBox.X + sendBox.Width);
    sendInset.ShouldBeGreaterThanOrEqualTo(0);
    sendInset.ShouldBeLessThanOrEqualTo(24);
    // Task 346: Tag sits in the lead slot, on the send row, below the textarea.
    LocatorBoundingBoxResult tagBox = await BoxAsync(page, ".twe-agent-ask [data-qa=AskTag]");
    ((double)tagBox.Y).ShouldBeGreaterThanOrEqualTo(areaBox.Y + areaBox.Height - 2);
    ((double)tagBox.X).ShouldBeLessThan(sendBox.X);

    await composer.ScreenshotAsync(new LocatorScreenshotOptions
    {
      Path = ScreenshotPath("ask-composer-before.png"),
    });

    double before = areaBox.Height;
    await textarea.ClickAsync();
    await textarea.PressSequentiallyAsync("one");
    await textarea.PressAsync("Shift+Enter");
    await textarea.PressSequentiallyAsync("two");
    await textarea.PressAsync("Shift+Enter");
    await textarea.PressSequentiallyAsync("three");
    string draft = await textarea.InputValueAsync();
    draft.ShouldContain("one");
    draft.ShouldContain("\n");
    draft.ShouldContain("three");
    LocatorBoundingBoxResult grown = await WaitForBoxAsync(
      page,
      areaSelector,
      box => box.Height >= before + 8);
    ((double)grown.Height).ShouldBeGreaterThan(before);
    await composer.ScreenshotAsync(new LocatorScreenshotOptions
    {
      Path = ScreenshotPath("ask-composer-after.png"),
    });

    await textarea.PressAsync("Enter");
    await page.Locator(".sc-ai-message--user").GetByText("one").WaitForAsync(new LocatorWaitForOptions
    {
      Timeout = 30_000,
    });
    await page.Locator(".sc-ai-root").GetByText("page_context:").WaitForAsync(new LocatorWaitForOptions
    {
      Timeout = 60_000,
    });
    await WaitUntilInputEnabledAsync(page);
  }

  internal static async Task AssertAtPickerWithNoTypesAsync(IPage page)
  {
    // The SPA is signed out here (the mock principal header authenticates API calls only), so
    // the picker opens with no types. AskSignIn_Given_Wasm proves the signed-in flow.
    ILocator textarea = page.Locator("textarea.sc-ai-input__textarea");
    await textarea.ClickAsync();
    await textarea.PressSequentiallyAsync("@");
    ILocator picker = page.Locator("[data-qa=AskReferencePicker]");
    await picker.WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
    await page.Locator("[data-qa=AskReferenceNoTypes]").WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
    (await page.EvaluateAsync<string?>("() => document.activeElement?.getAttribute('data-qa')"))
      .ShouldBe("AskReferenceInput");
    await page.Keyboard.PressAsync("Escape");
    await picker.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = 30_000 });
    (await textarea.InputValueAsync()).ShouldBe("@");
    // Close renders the picker away first; FocusComposerAsync refocuses the textarea after, so wait for it.
    await page.WaitForFunctionAsync(
      "() => document.activeElement?.classList.contains('sc-ai-input__textarea') === true",
      null,
      new PageWaitForFunctionOptions { Timeout = 10_000 });

    // A mid-word @ (an email address) stays text: no picker, focus stays in the textarea.
    await textarea.FillAsync("");
    await textarea.PressSequentiallyAsync("ada@example.com");
    (await textarea.InputValueAsync()).ShouldBe("ada@example.com");
    (await picker.IsVisibleAsync()).ShouldBeFalse();
    await textarea.FillAsync("");
  }

  internal static async Task WaitUntilInputEnabledAsync(IPage page)
  {
    ILocator textarea = page.Locator("textarea.sc-ai-input__textarea");
    for (int attempt = 0; attempt < 100; attempt++)
    {
      if (await textarea.GetAttributeAsync("disabled") is null)
      {
        return;
      }

      await Task.Delay(100);
    }

    throw new TimeoutException("Ask textarea stayed disabled after Enter");
  }

  internal static async Task AssertDockedPanelAsync(IPage page)
  {
    // Close the @ picker if it is still open, so the state shots show the docked panel.
    ILocator picker = page.Locator("[data-qa=AskReferencePicker]");
    if (await picker.CountAsync() > 0 && await picker.IsVisibleAsync())
    {
      await page.Locator("[data-qa=AskReferenceClose]").ClickAsync();
      await picker.WaitForAsync(new LocatorWaitForOptions
      {
        State = WaitForSelectorState.Hidden,
        Timeout = 30_000,
      });
    }

    await page.SetViewportSizeAsync(1280, 800);
    await AssertIconButtonAsync(page, "AskNewConversation", "New conversation");
    await AssertIconButtonAsync(page, "AskExpand", "Expand");
    await AssertIconButtonAsync(page, "AgentAskClose", "Close");
    await AssertHeaderActionsSitBesideTheTitleAsync(page);
    await AssertEditModePillBesideTagAsync(page);

    double width = page.ViewportSize!.Width;
    LocatorBoundingBoxResult panel = await WaitForBoxAsync(page, "[data-qa=AgentAsk]", box =>
      Math.Abs(box.X - (width - 450)) <= 8
      && Math.Abs(box.X + box.Width - width) <= 2
      && Math.Abs(box.Width - 450) <= 2);
    LocatorBoundingBoxResult layout = await BoxAsync(page, ".twe-shell__layout");
    LocatorBoundingBoxResult content = await BoxAsync(page, ".fluent-layout-item[area=content]");
    layout.Width.ShouldBeGreaterThan(800);
    content.Width.ShouldBeGreaterThan(400);
    (await page.Locator(".fluent-layout").GetAttributeAsync("mobile")).ShouldBeNull();
    ((double)panel.X).ShouldBe(width - 450, 8);
    ((double)(panel.X + panel.Width)).ShouldBe(width, 2);
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("ask-docked-1280.png") });

    await page.Locator("[data-qa=AskExpand]").ClickAsync();
    await AssertIconButtonAsync(page, "AskExpand", "Collapse");
    double height = page.ViewportSize!.Height;
    LocatorBoundingBoxResult expanded = await WaitForBoxAsync(page, "[data-qa=AgentAsk]", box =>
      box.X <= 2
      && box.Y <= 2
      && Math.Abs(box.Width - width) <= 2
      && Math.Abs(box.Height - height) <= 2);
    ((double)expanded.Width).ShouldBe(width, 2);
    ((double)expanded.Height).ShouldBe(height, 2);
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("ask-expanded-1280.png") });

    await page.Locator("[data-qa=AskExpand]").ClickAsync();
    await AssertIconButtonAsync(page, "AskExpand", "Expand");
    await WaitForBoxAsync(page, "[data-qa=AgentAsk]", box => Math.Abs(box.X - (width - 450)) <= 8);

    await page.SetViewportSizeAsync(800, 700);
    LocatorBoundingBoxResult narrow = await WaitForBoxAsync(page, "[data-qa=AgentAsk]", box =>
      box.X <= 2
      && box.Y <= 2
      && Math.Abs(box.Width - 800) <= 2
      && Math.Abs(box.Height - 700) <= 2);
    ((double)narrow.Width).ShouldBe(800, 2);
    ((double)narrow.Height).ShouldBe(700, 2);
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("ask-narrow-800.png") });

    await page.SetViewportSizeAsync(1280, 800);
    await page.Locator("[data-qa=AgentAskClose]").ClickAsync();
    await page.Locator("[data-qa=AgentAsk]").WaitForAsync(new LocatorWaitForOptions
    {
      State = WaitForSelectorState.Hidden,
      Timeout = 30_000,
    });
    LocatorBoundingBoxResult closed = await WaitForBoxAsync(page, ".twe-shell__layout", box =>
      box.X <= 2 && Math.Abs(box.Width - page.ViewportSize!.Width) <= 2);
    ((double)closed.Width).ShouldBe(page.ViewportSize!.Width, 2);
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = ScreenshotPath("ask-closed-1280.png") });

    await page.Locator("[data-qa=AskAiButton]").ClickAsync();
    await page.Locator("[data-qa=AgentAsk]").WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
    await WaitForBoxAsync(page, "[data-qa=AgentAsk]", box =>
      Math.Abs(box.X - (page.ViewportSize!.Width - 450)) <= 8);
  }

  internal static async Task AssertIconButtonAsync(IPage page, string qa, string accessibleName)
  {
    ILocator button = page.Locator($"[data-qa={qa}]");
    (await button.GetAttributeAsync("aria-label")).ShouldBe(accessibleName, qa);
    (await button.GetAttributeAsync("title")).ShouldBe(accessibleName, qa);
    LocatorBoundingBoxResult? icon = await button.Locator("svg").First.BoundingBoxAsync();
    icon.ShouldNotBeNull(qa);
    ((double)icon.Width).ShouldBeGreaterThanOrEqualTo(16, qa);
    ((double)icon.Height).ShouldBeGreaterThanOrEqualTo(16, qa);
  }

}
