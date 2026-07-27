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
function base64UrlToBuffer(value) {
    const padLength = (4 - (value.length % 4)) % 4;
    const padded = (value + "=".repeat(padLength)).replace(/-/g, "+").replace(/_/g, "/");
    const binary = atob(padded);
    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) {
        bytes[i] = binary.charCodeAt(i);
    }
    return bytes.buffer;
}
function bufferToBase64Url(buffer) {
    const bytes = new Uint8Array(buffer);
    let binary = "";
    for (let i = 0; i < bytes.length; i++) {
        binary += String.fromCharCode(bytes[i]);
    }
    return btoa(binary).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
}
export const WebAuthn = {
    IsSupported: () => typeof window.PublicKeyCredential !== "undefined",
    // Returns a JSON string: { credentialId, clientDataJson, attestationObject } — matching
    // CompletePasskeyRegistration.Command's field names.
    CreateCredential: async (optionsJson) => {
        const options = JSON.parse(optionsJson);
        const publicKey = {
            ...options,
            challenge: base64UrlToBuffer(options.challenge),
            user: {
                ...options.user,
                id: base64UrlToBuffer(options.user.id),
            },
        };
        const credential = (await navigator.credentials.create({ publicKey }));
        const response = credential.response;
        return JSON.stringify({
            credentialId: bufferToBase64Url(credential.rawId),
            clientDataJson: bufferToBase64Url(response.clientDataJSON),
            attestationObject: bufferToBase64Url(response.attestationObject),
        });
    },
    // Returns a JSON string: { credentialId, clientDataJson, authenticatorData, signature,
    // userHandle } — matching CompletePasskeyAuthentication.Command's field names. userHandle is
    // null when the authenticator did not return one.
    GetCredential: async (optionsJson) => {
        const options = JSON.parse(optionsJson);
        const publicKey = {
            ...options,
            challenge: base64UrlToBuffer(options.challenge),
        };
        const credential = (await navigator.credentials.get({ publicKey }));
        const response = credential.response;
        return JSON.stringify({
            credentialId: bufferToBase64Url(credential.rawId),
            clientDataJson: bufferToBase64Url(response.clientDataJSON),
            authenticatorData: bufferToBase64Url(response.authenticatorData),
            signature: bufferToBase64Url(response.signature),
            userHandle: response.userHandle ? bufferToBase64Url(response.userHandle) : null,
        });
    },
};
//# sourceMappingURL=web-authn.js.map