// web-authn.ts — thin browser WebAuthn (passkey) bridge exposed as window.Spa.WebAuthn.
// Converts the server-minted options JSON (challenge/user.id as base64url strings — see
// WebAuthnRegistration/WebAuthnAuthentication.BuildOptionsJson) into the ArrayBuffer shapes
// navigator.credentials.create/get require, and converts the browser's binary response back into
// base64url strings matching the CompletePasskeyRegistration/CompletePasskeyAuthentication
// contracts' field names.
//
// Deliberately hand-rolled base64url<->ArrayBuffer conversion rather than the newer WebAuthn
// Level 3 convenience APIs (PublicKeyCredential.parseCreationOptionsFromJSON/.toJSON()): those
// trail both browser support and the TypeScript DOM lib snapshot bundled with this project's pinned
// compiler version, whereas navigator.credentials.create/get and ArrayBuffer plumbing are long
// stable. This mirrors the server verifier's own "no exotic/newest APIs" posture.
// Plain object (not a class): Blazor's string-identifier JS interop resolver requires every
// intermediate path segment to be typeof "object" (see spa.ts).

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

function assertionToJson(credential: PublicKeyCredential): string {
  const response = credential.response as AuthenticatorAssertionResponse;
  return JSON.stringify({
    credentialId: bufferToBase64Url(credential.rawId),
    clientDataJson: bufferToBase64Url(response.clientDataJSON),
    authenticatorData: bufferToBase64Url(response.authenticatorData),
    signature: bufferToBase64Url(response.signature),
    userHandle: response.userHandle ? bufferToBase64Url(response.userHandle) : null,
  });
}

function toPublicKeyRequest(options: RequestOptionsJson): PublicKeyCredentialRequestOptions {
  return {
    ...options,
    challenge: base64UrlToBuffer(options.challenge),
  } as unknown as PublicKeyCredentialRequestOptions;
}

/** Active AbortController for a pending conditional get (autofill). */
let conditionalAbort: AbortController | null = null;

export const WebAuthn = {
  IsSupported: (): boolean => typeof window.PublicKeyCredential !== "undefined",

  // Feature-detect conditional UI (passkey autofill). Prefer isConditionalMediationAvailable;
  // fall back to getClientCapabilities when present.
  IsConditionalMediationAvailable: async (): Promise<boolean> => {
    try {
      const pk = window.PublicKeyCredential as typeof PublicKeyCredential & {
        isConditionalMediationAvailable?: () => Promise<boolean>;
        getClientCapabilities?: () => Promise<Record<string, boolean>>;
      };
      if (typeof pk?.isConditionalMediationAvailable === "function") {
        return await pk.isConditionalMediationAvailable();
      }
      if (typeof pk?.getClientCapabilities === "function") {
        const caps = await pk.getClientCapabilities();
        return caps.conditionalGet === true;
      }
    } catch {
      /* ignore */
    }
    return false;
  },

  // Returns a JSON string: { credentialId, clientDataJson, attestationObject } — matching
  // CompletePasskeyRegistration.Command's field names.
  // preferHybrid: optional; when true, sets hints: ["hybrid"] for cross-device focused UI.
  CreateCredential: async (optionsJson: string, preferHybrid: boolean = false): Promise<string> => {
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
  },

  // Modal get. preferHybrid forces hybrid-first hints.
  // Aborts any pending conditional get first so the modal request can run.
  GetCredential: async (optionsJson: string, preferHybrid: boolean = false): Promise<string> => {
    if (conditionalAbort) {
      conditionalAbort.abort();
      conditionalAbort = null;
    }

    const options: RequestOptionsJson = applyHybridPreference(JSON.parse(optionsJson), preferHybrid);
    const credential = (await navigator.credentials.get({
      publicKey: toPublicKeyRequest(options),
    })) as PublicKeyCredential;

    return assertionToJson(credential);
  },

  // Conditional UI (passkey form autofill) — does NOT show a modal. Stays pending until the user
  // picks a passkey (or "Passkeys from a Nearby Device") from the autofill dropdown on an input
  // with autocomplete="username webauthn". See web.dev passkey-form-autofill / task 166.
  // Returns assertion JSON, or null if aborted (modal took over / page dispose).
  GetCredentialConditional: async (optionsJson: string): Promise<string | null> => {
    if (conditionalAbort) {
      conditionalAbort.abort();
    }
    conditionalAbort = new AbortController();
    const signal = conditionalAbort.signal;

    try {
      const options: RequestOptionsJson = JSON.parse(optionsJson);
      const credential = (await navigator.credentials.get({
        publicKey: toPublicKeyRequest(options),
        mediation: "conditional",
        signal,
      })) as PublicKeyCredential;

      conditionalAbort = null;
      return assertionToJson(credential);
    } catch (error) {
      conditionalAbort = null;
      const name = error instanceof DOMException ? error.name : "";
      // AbortError: modal path or dispose cancelled us — not a user-facing failure.
      if (name === "AbortError") {
        return null;
      }
      throw error;
    }
  },

  AbortConditional: (): void => {
    if (conditionalAbort) {
      conditionalAbort.abort();
      conditionalAbort = null;
    }
  },
};
