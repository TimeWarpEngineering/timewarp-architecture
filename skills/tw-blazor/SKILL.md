---
name: tw-blazor
description: "Razor file authoring — one @code at the top, markup, optional <style> last. Use when creating or editing .razor files, @code blocks, or in-file <style> tags. CSS placement: tw-blazor-css-strategy. App shell: tw-blazor-layout."
---

# `.razor` file order

1. Directives (`@namespace`, `@inherits`, `@using`, `@inject`, comments)
2. **One** `@code { … }` — omit if none or code-behind only
3. Markup
4. Optional `<style>` last (Exception B — `tw-blazor-css-strategy`)

Never two `@code` blocks. Never `@code` after markup. Never `<style>` above markup.

```razor
@namespace TimeWarp.Architecture.Features.Example
@inherits BaseComponent

@code {
  [Parameter] public string? Title { get; set; }
}

<div class="twe-example">@Title</div>

<style>
  @(@"
    .twe-example { color: var(--twe-ink); }
  ")
</style>
```
