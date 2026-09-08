// Purpose: collocated FluentSelect module at the URL WASM import()s.
// Design: Live WASM JSInterop looks up Microsoft.FluentUI.Blazor.Select.SetComboBoxValue
// (rc.4 identifier, no Components) on the imported module. rc.5 C# also calls
// Microsoft.FluentUI.Blazor.Components.Select.Initialize / ClearValue via
// InvokeFluentVoidAsync. Publish both identifier trees. SetComboBoxValue is the
// rc.4 combobox _control.value write (205-004 closed-label UX). Remove when a
// Fluent UI package packs this JS and these identifiers. Do not vendor the rest
// of the nupkg.

export var Microsoft;
(function (Microsoft) {
  var FluentUI;
  (function (FluentUI) {
    var Blazor;
    (function (Blazor) {
      var Select;
      (function (Select) {
        function ClearValue(id) {
          const element = document.getElementById(id);
          if (element) {
            element.value = null;
          }
        }
        Select.ClearValue = ClearValue;

        function SetComboBoxValue(id, value) {
          const element = document.getElementById(id);
          if (element && element.tagName === 'FLUENT-DROPDOWN' && element._control) {
            element._control.value = value;
          }
        }
        Select.SetComboBoxValue = SetComboBoxValue;
      })(Select = Blazor.Select || (Blazor.Select = {}));

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
  root.Microsoft.FluentUI.Blazor.Select = Microsoft.FluentUI.Blazor.Select;
  root.Microsoft.FluentUI.Blazor.Components = root.Microsoft.FluentUI.Blazor.Components || {};
  root.Microsoft.FluentUI.Blazor.Components.Select = Microsoft.FluentUI.Blazor.Components.Select;
})(globalThis);
