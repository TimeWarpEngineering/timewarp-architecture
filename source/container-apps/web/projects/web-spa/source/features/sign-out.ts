// #region Purpose
// Browser-side sign-out: fetch the antiforgery token, then submit a real form POST so the browser
// (not a server loopback) receives the cookie deletion and follows the 303 to /Login.
// #endregion
//
// #region Design
// Task 251: under InteractiveServer the SPA sign-out handler runs in the circuit; any HTTP call it
// makes is server→server and its Set-Cookie never reaches the browser. This module runs in the
// browser in every render mode, so both requests carry and update the browser's own cookie jar.
// Form submit (not fetch + location.assign) because the POST response IS the navigation: one full
// page load that re-reads the cookie and drops the Server circuit. Paths come from C#
// (SignOutBrowserSession) so this file holds no route strings. Throws on a non-OK token read so
// the C# caller can fall back; imported on demand (SignOutJsModule), not via window.Spa.
// #endregion

interface AntiforgeryToken {
  formFieldName: string;
  requestToken: string;
}

export async function SignOut(antiforgeryTokenPath: string, signOutPath: string): Promise<void> {
  const response = await fetch(antiforgeryTokenPath, {
    method: "GET",
    credentials: "same-origin",
    cache: "no-store",
    headers: { Accept: "application/json" },
  });
  if (!response.ok) {
    throw new Error(`Sign-out antiforgery token request failed: ${response.status}`);
  }

  const token = (await response.json()) as AntiforgeryToken;

  const form = document.createElement("form");
  form.method = "post";
  form.action = signOutPath;
  form.style.display = "none";

  const field = document.createElement("input");
  field.type = "hidden";
  field.name = token.formFieldName;
  field.value = token.requestToken;
  form.appendChild(field);

  document.body.appendChild(form);
  form.submit();
}
