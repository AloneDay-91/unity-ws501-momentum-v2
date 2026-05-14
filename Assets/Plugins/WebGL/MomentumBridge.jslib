mergeInto(LibraryManager.library, {
  MomentumPostQuit: function (sessionIdPtr) {
    var sessionId = UTF8ToString(sessionIdPtr);
    console.log('[MomentumBridge] quit requested, sessionId=', sessionId);
    try {
      // Direct navigation of the parent window — bypasses any React listener
      // that may have been unmounted by Fast Refresh / re-render. We're same-origin
      // (iframe served by Next.js), so cross-origin writes are not an issue.
      if (window.parent && window.parent !== window) {
        window.parent.location.href = '/classement/' + encodeURIComponent(sessionId);
      } else {
        // Not in an iframe (standalone webgl page) — navigate the current window.
        window.location.href = '/classement/' + encodeURIComponent(sessionId);
      }
    } catch (e) {
      console.error('[MomentumBridge] navigation failed, falling back to postMessage', e);
      try {
        window.parent.postMessage({ type: 'momentum-quit', sessionId: sessionId }, '*');
      } catch (e2) {
        console.error('[MomentumBridge] postMessage fallback also failed', e2);
      }
    }
  }
});
