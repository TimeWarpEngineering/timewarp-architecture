// Purpose: collocated FluentSelect module at the URL WASM import()s.
// Design: Microsoft.FluentUI.AspNetCore.Components 5.0.0-rc.5-26219.1 C# still
// import()s Components/List/FluentSelect.razor.js and calls
// Microsoft.FluentUI.Blazor.Components.Select.Initialize / ClearValue, but the
// nupkg does not pack that file (Fluent UI #5074 moved the source into
// Core.Scripts / lib.module.js and deleted the collocated .razor.ts). This
// overlay restores the module MapStaticAssets serves at that exact _content
// path. Body matches rc.5 FluentSelect.ts (combobox _control text + a11y).
// Also publish on globalThis so IJSRuntime.InvokeVoidAsync finds the same
// identifiers. Remove when a Fluent UI package packs this JS again. Do not
// vendor the rest of the nupkg.

export var Microsoft;
(function (Microsoft) {
  var FluentUI;
  (function (FluentUI) {
    var Blazor;
    (function (Blazor) {
      var Components;
      (function (Components) {
        var Select;
        (function (Select) {
          function ClearValue(id) {
            const element = document.getElementById(id);
            if (element) {
              element.value = null;
            }
          }
          Select.ClearValue = ClearValue;

          function Initialize(id, defaultValue) {
            const element = document.getElementById(id);
            if (!element) {
              return;
            }

            if (element.getAttribute('type') === 'combobox' && element.tagName === 'FLUENT-DROPDOWN' && element._control) {
              element._control.value = defaultValue;
            }

            const controlElement = element.querySelector('button[slot=control], input[slot=control]');
            if (controlElement) {
              if (!controlElement.hasAttribute('aria-label')) {
                controlElement.setAttribute('aria-label', getAccessibleLabel(element));
              }
              if (!controlElement.hasAttribute('aria-expanded')) {
                controlElement.setAttribute('aria-expanded', 'false');
              }
            }
          }
          Select.Initialize = Initialize;

          function getAccessibleLabel(element) {
            const label = element.closest('fluent-field')?.querySelector('label');
            if (label?.textContent?.trim()) {
              return label.textContent.trim();
            }

            const placeholder = element.getAttribute('placeholder');
            if (placeholder?.trim()) {
              return placeholder.trim();
            }

            return 'Select an option';
          }
        })(Select = Components.Select || (Components.Select = {}));
      })(Components = Blazor.Components || (Blazor.Components = {}));
    })(Blazor = FluentUI.Blazor || (FluentUI.Blazor = {}));
  })(FluentUI = Microsoft.FluentUI || (Microsoft.FluentUI = {}));
})(Microsoft || (Microsoft = {}));

(function publishSelectOnGlobalThis(root) {
  root.Microsoft = root.Microsoft || {};
  root.Microsoft.FluentUI = root.Microsoft.FluentUI || {};
  root.Microsoft.FluentUI.Blazor = root.Microsoft.FluentUI.Blazor || {};
  root.Microsoft.FluentUI.Blazor.Components = root.Microsoft.FluentUI.Blazor.Components || {};
  root.Microsoft.FluentUI.Blazor.Components.Select = Microsoft.FluentUI.Blazor.Components.Select;
})(globalThis);
