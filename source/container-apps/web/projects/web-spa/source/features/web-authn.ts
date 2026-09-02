// #region Purpose
// Thin browser WebAuthn (passkey) bridge: named exports for on-demand IJSRuntime import(), plus
// a window.Spa.WebAuthn object for optional string-identifier callers.
// #endregion
//
// #region Design
// Converts server-minted options JSON (challenge/user.id as base64url — see
// WebAuthnRegistration/WebAuthnAuthentication.BuildOptionsJson) into the ArrayBuffer shapes
// navigator.credentials.create/get require, and converts the browser binary response back into
// base64url strings matching CompletePasskeyRegistration/CompletePasskeyAuthentication fields.
// Hand-rolled base64url<->ArrayBuffer rather than WebAuthn Level 3 parseCreationOptionsFromJSON /
// toJSON: those trail browser support and this project's TypeScript DOM lib snapshot.
// Task 200: Login/Settings passkey C# imports this module (`./js/features/web-authn.js`) and
// calls the named exports. That path does not require window.Spa. The WebAuthn object remains
// on Spa for Counter-style string identifiers; the host JS initializer still must list Web.Spa
// for Spa.Counter.* (asserted on web-server).
// #endregion

function base64UrlToBuffer(value: string): ArrayBuffer {
  const padLength = (4 - (value.length % 4)) % 4;
  const padded = (value + "=".repeat(padLength)).replace(/-/g, "+").replace(/_/g, "/");
  const binary = atob(padded);
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i++) {
    bytes[i] = binary.charCodeAt(i);
  }
  return bytes.buffer;
}

function bufferToBase64Url(buffer: ArrayBuffer): string {
  const bytes = new Uint8Array(buffer);
  let binary = "";
  for (let i = 0; i < bytes.length; i++) {
    binary += String.fromCharCode(bytes[i]);
  }
  return btoa(binary).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
}

interface CreationOptionsJson {
  challenge: string;
  rp: { id: string; name: string };
  user: { id: string; name: string; displayName: string };
  pubKeyCredParams: { type: string; alg: number }[];
  authenticatorSelection?: { residentKey?: string; userVerification?: string };
  attestation?: string;
  timeout?: number;
  /** WebAuthn Level 3 client hints: "client-device" | "hybrid" | "security-key" */
  hints?: string[];
}

interface RequestOptionsJson {
  challenge: string;
  rpId: string;
  allowCredentials: unknown[];
  userVerification?: string;
  timeout?: number;
  /** WebAuthn Level 3 client hints: "client-device" | "hybrid" | "security-key" */
  hints?: string[];
}

/** When preferHybrid, force hints to hybrid-only so Chrome prioritizes nearby-phone / QR UI. */
function applyHybridPreference<T extends { hints?: string[] }>(options: T, preferHybrid: boolean): T {
  if (!preferHybrid) {
    return options;
  }
  return { ...options, hints: ["hybrid"] };
}

export function IsSupported(): boolean {
  return typeof window.PublicKeyCredential !== "undefined";
}

// Returns a JSON string: { credentialId, clientDataJson, attestationObject } — matching
// CompletePasskeyRegistration.Command's field names.
// preferHybrid: optional; when true, sets hints: ["hybrid"] for cross-device focused UI.
export async function CreateCredential(
  optionsJson: string,
  preferHybrid: boolean = false,
): Promise<string> {
  const options: CreationOptionsJson = applyHybridPreference(JSON.parse(optionsJson), preferHybrid);

  const publicKey = {
    ...options,
    challenge: base64UrlToBuffer(options.challenge),
    user: {
      ...options.user,
      id: base64UrlToBuffer(options.user.id),
    },
  } as unknown as PublicKeyCredentialCreationOptions;

  const credential = (await navigator.credentials.create({ publicKey })) as PublicKeyCredential;
  const response = credential.response as AuthenticatorAttestationResponse;

  return JSON.stringify({
    credentialId: bufferToBase64Url(credential.rawId),
    clientDataJson: bufferToBase64Url(response.clientDataJSON),
    attestationObject: bufferToBase64Url(response.attestationObject),
  });
}

// Returns a JSON string: { credentialId, clientDataJson, authenticatorData, signature,
// userHandle } — matching CompletePasskeyAuthentication.Command's field names. userHandle is
// null when the authenticator did not return one.
// preferHybrid: optional; when true, sets hints: ["hybrid"] for "Passkeys from a nearby device".
export async function GetCredential(
  optionsJson: string,
  preferHybrid: boolean = false,
): Promise<string> {
  const options: RequestOptionsJson = applyHybridPreference(JSON.parse(optionsJson), preferHybrid);

  const publicKey = {
    ...options,
    challenge: base64UrlToBuffer(options.challenge),
  } as unknown as PublicKeyCredentialRequestOptions;

  const credential = (await navigator.credentials.get({ publicKey })) as PublicKeyCredential;
  const response = credential.response as AuthenticatorAssertionResponse;

  return JSON.stringify({
    credentialId: bufferToBase64Url(credential.rawId),
    clientDataJson: bufferToBase64Url(response.clientDataJSON),
    authenticatorData: bufferToBase64Url(response.authenticatorData),
    signature: bufferToBase64Url(response.signature),
    userHandle: response.userHandle ? bufferToBase64Url(response.userHandle) : null,
  });
}

export const WebAuthn = {
  IsSupported,
  CreateCredential,
  GetCredential,
};
