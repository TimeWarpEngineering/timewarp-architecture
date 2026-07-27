# Web.Spa frontend

- **TypeScript**: `source/*.ts` compiles to `wwwroot/js` on build via
  `Microsoft.TypeScript.MSBuild` — no npm/node required.
- **Styling**: plain CSS driven by design tokens (`wwwroot/css/tokens.css` + `app.css`)
  plus Blazor scoped CSS per component. The Tailwind/npm toolchain was removed.

See the `blazor-css-strategy` skill for the design-token + scoped-CSS approach.
