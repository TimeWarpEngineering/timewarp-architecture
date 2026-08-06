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

export const WebAuthn = {
  IsSupported: (): boolean => typeof window.PublicKeyCredential !== "undefined",

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

  // Returns a JSON string: { credentialId, clientDataJson, authenticatorData, signature,
  // userHandle } — matching CompletePasskeyAuthentication.Command's field names. userHandle is
  // null when the authenticator did not return one.
  // preferHybrid: optional; when true, sets hints: ["hybrid"] for "Passkeys from a nearby device".
  GetCredential: async (optionsJson: string, preferHybrid: boolean = false): Promise<string> => {
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
  },
};
