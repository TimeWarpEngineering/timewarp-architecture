# Apply tw-blazor file order to non-compliant razor files

## Description

20 `.razor` files fail `tw-blazor` (directives, one `@code` at the top, markup,
optional `<style>` last). Reorder only — no behavior change.

## Requirements

- One `@code` immediately after directives/comments
- Merge split `@code` blocks (PrincipalsPage, RolesListPage)
- Move bottom `@code` above markup on the other 18
- Do not change C# or markup contents

## Checklist

- [x] Merge PrincipalsPage and RolesListPage `@code` blocks
- [x] Move `@code` above markup on the 18 after-markup files
- [x] Re-scan all `.razor` files — 0 violations
- [x] Commit

## Notes

Skill: `tw-blazor`. Review found 20 of 64 non-compliant.

## Results

All 20 files now follow directives → one `@code` → markup. Re-scan of 64
`.razor` files: 0 violations. Content unchanged; order only.

### How to validate

```bash
rg -l '@code' --glob '*.razor' -g '!**/obj/**' -g '!**/bin/**' | while read -r f; do
  n=$(rg -c '@code' "$f")
  first=$(rg -n '@code|^[[:space:]]*<(?!style)' "$f" | head -1)
  if [ "$n" -gt 1 ]; then echo "MULTI $f ($n)"; fi
  echo "$first" | rg -q '@code' || echo "CODE-AFTER-MARKUP $f"
done
# Expect: no MULTI, no CODE-AFTER-MARKUP
```

No runtime/UI change — Blazor compiles `@code` the same regardless of position.

## Session

- Implementation: grok 2026-08-13
