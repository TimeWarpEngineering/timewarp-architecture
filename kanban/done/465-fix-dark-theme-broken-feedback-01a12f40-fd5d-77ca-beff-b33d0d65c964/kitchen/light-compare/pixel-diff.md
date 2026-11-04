# Light theme pixel comparison (task 465)

Each walk signs in a fresh passkey account (light OS scheme, reduced motion, 1440x900) and
shoots all 19 PageRegistry pages. "Before" is web-spa at eb4e15666; "after" is the fix commit.
Only the four images above are kept. The full sets were in /tmp.

| Page | Pixels that differ | Where | Cause |
|------|--------------------|-------|-------|
| every page | 841 | x 1310-1341, y 15-46 | app-bar avatar, generated randomly per new account |
| home, profile, passkeys | one 32-56px box | avatar in the page body | same random avatar |
| settings, passkeys, agent links, principals, claims | small text boxes | ids and timestamps | per-run account id, credential id, created time |
| design system | x 573-1244, y 670-832 | Color Tokens labels | intended: the hex copy became the token name (`--twe-ink`) |

No other pixel changed. Light-mode surfaces, text, badges, links, and nav are identical.
