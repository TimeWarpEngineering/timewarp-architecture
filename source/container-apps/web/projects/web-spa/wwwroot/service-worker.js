// #region Purpose
// Development service worker: a no-op fetch handler, so no offline support.
// #endregion
//
// #region Design
// In development, always fetch from the network and do not enable offline support.
// This is because caching would make development more difficult (changes would not
// be reflected on the first load after each change).
// #endregion
self.addEventListener("fetch", () => {});
