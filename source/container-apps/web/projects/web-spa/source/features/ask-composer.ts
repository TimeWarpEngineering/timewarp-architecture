// #region Purpose
// The Ask composer textarea selector, shared by ask-ai.ts and ask-voice.ts.
// #endregion
//
// #region Design
// The library MessageInput renders the textarea; the panel root class scopes it to Ask. One copy
// so the @ insert, focus, and dictation code always find the same element.
// #endregion

export const ComposerSelector = ".twe-agent-ask .sc-ai-input__textarea";
