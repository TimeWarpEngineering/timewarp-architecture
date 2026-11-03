#region Purpose
// Interaction assertions for the Ask surface Playwright proof: header actions, edit mode, settings
// menu, copy tooltip, clipboard and reference insertion.
#endregion

namespace AskSurfacePlaywright_;

using static AskSurfacePlaywright_.AskSurfaceLayoutAsserts;

internal static class AskSurfaceInteractionAsserts
{
  internal static async Task AssertHeaderActionsSitBesideTheTitleAsync(IPage page)
  {
    LocatorBoundingBoxResult title = await BoxAsync(page, ".twe-agent-ask__title");
    LocatorBoundingBoxResult actions = await BoxAsync(page, ".twe-agent-ask__header-actions");
    actions.X.ShouldBeGreaterThan(title.X + title.Width);
  }

  internal static async Task AssertEditModePillBesideTagAsync(IPage page)
  {
    LocatorBoundingBoxResult tag = await BoxAsync(page, ".twe-agent-ask [data-qa=AskTag]");
    LocatorBoundingBoxResult pill = await BoxAsync(page, ".twe-agent-ask [data-qa=AskEditMode]");
    ((double)pill.Y).ShouldBe(tag.Y, 4);
    pill.X.ShouldBeGreaterThan(tag.X + tag.Width - 2);
  }

  // Enter or Space opens, ArrowDown moves, Enter chooses; Esc closes without a change. Focus returns to the pill.
  internal static async Task AssertEditModeKeyboardAsync(IPage page)
  {
    ILocator pill = page.Locator("[data-qa=AskEditMode]");
    (await pill.GetAttributeAsync("aria-haspopup")).ShouldNotBeNull();
    (await pill.GetAttributeAsync("aria-label")).ShouldBe("Edit mode: Ask before editing");

    await pill.FocusAsync();
    await page.Keyboard.PressAsync("Enter");
    await AttributeShouldBeAsync(pill, "aria-expanded", "true");
    await ActiveShouldBeAsync(page, "AskBeforeEditing");
    await page.Keyboard.PressAsync("ArrowDown");
    await ActiveShouldBeAsync(page, "AskAutomaticallyEdit");
    await page.Keyboard.PressAsync("Enter");
    await ModeShouldBeAsync(page, "AutomaticallyEdit");
    await AttributeShouldBeAsync(pill, "aria-expanded", "false");
    await ActiveShouldBeAsync(page, "AskEditMode");
    (await pill.GetAttributeAsync("aria-label")).ShouldBe("Edit mode: Automatically edit");

    await page.Keyboard.PressAsync("Enter");
    await AttributeShouldBeAsync(pill, "aria-expanded", "true");
    await page.Keyboard.PressAsync("Escape");
    await AttributeShouldBeAsync(pill, "aria-expanded", "false");
    await ActiveShouldBeAsync(page, "AskEditMode");
    await ModeShouldBeAsync(page, "AutomaticallyEdit");

    await page.Keyboard.PressAsync("Space");
    await AttributeShouldBeAsync(pill, "aria-expanded", "true");
    await page.Keyboard.PressAsync("Escape");
    await AttributeShouldBeAsync(pill, "aria-expanded", "false");
    await ActiveShouldBeAsync(page, "AskEditMode");

    await page.Keyboard.PressAsync("Enter");
    await AttributeShouldBeAsync(pill, "aria-expanded", "true");
    await page.Keyboard.PressAsync("Home");
    await ActiveShouldBeAsync(page, "AskBeforeEditing");
    await page.Keyboard.PressAsync("Enter");
    await ModeShouldBeAsync(page, "AskBeforeEditing");
    await page.Locator("[data-qa=AgentAsk][data-built-mode=AskBeforeEditing]").WaitForAsync(new LocatorWaitForOptions
    {
      Timeout = 30_000,
    });
  }

  internal static async Task AssertSettingsMenuAsync(IPage page)
  {
    ILocator settings = page.Locator("[data-qa=AskSettings]");
    LocatorBoundingBoxResult button = await BoxAsync(page, ".twe-agent-ask [data-qa=AskSettings]");
    LocatorBoundingBoxResult send = await BoxAsync(page, ".twe-agent-ask .sc-ai-input__send");
    (button.X + button.Width).ShouldBeLessThanOrEqualTo(send.X + 1);
    ((double)(button.Y + (button.Height / 2))).ShouldBe(send.Y + (send.Height / 2), 6);
    (await settings.GetAttributeAsync("aria-haspopup")).ShouldNotBeNull();
    (await settings.GetAttributeAsync("aria-label")).ShouldBe("Akili settings");
    (await settings.GetAttributeAsync("data-status")).ShouldBe("Absent");

    await settings.FocusAsync();
    await page.Keyboard.PressAsync("Enter");
    await AttributeShouldBeAsync(settings, "aria-expanded", "true");
    ILocator status = page.Locator("[data-qa=AskAgentAccessStatus]");
    await status.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10_000 });
    (await status.InnerTextAsync()).ShouldContain("No agent credential");
    ILocator configure = page.Locator("[data-qa=AskConfigurePermissions]");
    (await configure.IsVisibleAsync()).ShouldBeTrue();
    await ShowCopyTooltipMustKeepMenuOpenAsync(page, "AskConfigurePermissions");
    await page.Locator("[data-qa=AgentAsk]").ScreenshotAsync(new LocatorScreenshotOptions
    {
      Path = ScreenshotPath("ask-settings-menu.png"),
    });
    await page.Keyboard.PressAsync("Escape");
    await AttributeShouldBeAsync(settings, "aria-expanded", "false");
    await ActiveShouldBeAsync(page, "AskSettings");

    await settings.ClickAsync();
    await configure.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10_000 });
    await configure.ClickAsync();
    await page.Locator("[data-qa=AgentAsk]").WaitForAsync(new LocatorWaitForOptions
    {
      State = WaitForSelectorState.Detached,
      Timeout = 30_000,
    });
    await page.WaitForURLAsync(url => url.Contains("AgentLinks", StringComparison.OrdinalIgnoreCase), new PageWaitForURLOptions
    {
      Timeout = 30_000,
    });
  }

  // Showing the Copy tooltip must leave the open menu item visible. The tooltip is a hint popover.
  internal static async Task ShowCopyTooltipMustKeepMenuOpenAsync(IPage page, string menuItemQa)
  {
    string mode = await page.EvaluateAsync<string>(
      """
      (menuItemQa) => {
        const copy = document.querySelector("[data-qa=AskCopy]");
        const tooltip = [...document.querySelectorAll("fluent-tooltip")]
          .find((element) => element.getAttribute("anchor") === copy?.id);
        if (!(tooltip instanceof HTMLElement) || copy === null) return "missing";
        const item = document.querySelector(`[data-qa="${CSS.escape(menuItemQa)}"]`);
        const list = item?.closest("[popover]");
        if (!tooltip.matches(":popover-open")) tooltip.showPopover();
        const menuOpen = !!(list && list.matches(":popover-open"));
        if (tooltip.matches(":popover-open")) tooltip.hidePopover();
        const popover = tooltip.getAttribute("popover") ?? "";
        return menuOpen ? popover : popover + ":closed";
      }
      """,
      menuItemQa);
    mode.ShouldBe("hint");
    (await page.Locator($"[data-qa={menuItemQa}]").IsVisibleAsync()).ShouldBeTrue();
  }

  internal static async Task ActiveShouldBeAsync(IPage page, string qa)
  {
    const string script = "() => document.activeElement?.getAttribute('data-qa')";
    string? active = null;
    for (int attempt = 0; attempt < 50; attempt++)
    {
      active = await page.EvaluateAsync<string?>(script);
      if (active == qa)
      {
        return;
      }

      await Task.Delay(100);
    }

    active.ShouldBe(qa);
  }

  internal static async Task ModeShouldBeAsync(IPage page, string mode) =>
    await AttributeShouldBeAsync(page.Locator("[data-qa=AskEditMode]"), "data-mode", mode);

  internal static async Task<LocatorBoundingBoxResult> WaitForBoxAsync(
    IPage page,
    string selector,
    Func<LocatorBoundingBoxResult, bool> ready)
  {
    LocatorBoundingBoxResult? last = null;
    for (int attempt = 0; attempt < 50; attempt++)
    {
      last = await page.Locator(selector).BoundingBoxAsync();
      if (last is not null && ready(last))
      {
        return last;
      }

      await Task.Delay(100);
    }

    string detail = last is null
      ? "no box"
      : $"x={last.X} y={last.Y} width={last.Width} height={last.Height}";
    throw new TimeoutException($"{selector} did not reach the expected box ({detail})");
  }

  internal static async Task AttributeShouldBeAsync(ILocator element, string attribute, string expected)
  {
    for (int attempt = 0; attempt < 50; attempt++)
    {
      if (await element.GetAttributeAsync(attribute) == expected)
      {
        return;
      }

      await Task.Delay(100);
    }

    (await element.GetAttributeAsync(attribute)).ShouldBe(expected, attribute);
  }

  internal static async Task AssertCopyWritesTheLastAnswerAsync(IPage page)
  {
    await page.Locator("[data-qa=AskCopy]").ClickAsync();
    string copied = "";
    for (int attempt = 0; attempt < 50 && !copied.StartsWith("page_context:", StringComparison.Ordinal); attempt++)
    {
      copied = await page.EvaluateAsync<string>("() => navigator.clipboard.readText()");
      await Task.Delay(100);
    }

    copied.ShouldStartWith("page_context:");
    copied.ShouldNotContain("Thumbs up");
    copied.ShouldNotContain("What is on this page?");
  }

  internal static async Task AssertCopyConversationWritesBothTurnsAsync(IPage page)
  {
    await page.EvaluateAsync("() => navigator.clipboard.writeText('')");
    await page.Locator("[data-qa=AskCopyConversation]").ClickAsync();
    string copied = "";
    for (int attempt = 0; attempt < 50 && !copied.StartsWith("You:", StringComparison.Ordinal); attempt++)
    {
      copied = await page.EvaluateAsync<string>("() => navigator.clipboard.readText()");
      await Task.Delay(100);
    }

    copied.ShouldStartWith("You:\none\ntwo\nthree\n\n");
    int second = copied.IndexOf("\n\nYou:\nWhat is on this page?\n\n", StringComparison.Ordinal);
    second.ShouldBeGreaterThan(0, copied);
    // Each turn has one "{AssistantName}:" paragraph whose answer starts with page_context:.
    (copied[..second].Split(":\npage_context:").Length - 1).ShouldBe(1, copied);
    (copied[second..].Split(":\npage_context:").Length - 1).ShouldBe(1, copied);
    copied.ShouldNotContain("Thumbs up");
  }

  internal static async Task AssertReferenceInsertReplacesTypedAtAsync(IPage page)
  {
    const string script = """
      async () => {
        const area = document.querySelector(".twe-agent-ask .sc-ai-input__textarea");
        area.value = "see @";
        area.selectionStart = area.value.length;
        area.selectionEnd = area.value.length;
        const module = await import(new URL("./js/features/ask-ai.js", document.baseURI).href);
        module.InsertReference("@profile:ada");
        const replaced = area.value;
        area.value = "see ";
        area.selectionStart = area.value.length;
        area.selectionEnd = area.value.length;
        module.InsertReference("@profile:ada");
        const appended = area.value;
        area.value = "see @ now";
        area.selectionStart = 5;
        area.selectionEnd = 5;
        module.InsertReference("@profile:ada");
        const beforeSpace = area.value;
        area.value = "";
        area.dispatchEvent(new Event("input", { bubbles: true }));
        return replaced + "|" + appended + "|" + beforeSpace;
      }
      """;
    string result = await page.EvaluateAsync<string>(script);
    // One space follows the token unless whitespace already does.
    result.ShouldBe("see @profile:ada |see @profile:ada |see @profile:ada now");
  }

  internal static async Task<LocatorBoundingBoxResult> BoxAsync(IPage page, string selector)
  {
    LocatorBoundingBoxResult? box = await page.Locator(selector).BoundingBoxAsync();
    box.ShouldNotBeNull(selector);
    return box;
  }
}
