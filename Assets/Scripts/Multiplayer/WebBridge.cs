#if WEB_BUILD
using System.Runtime.InteropServices;
using UnityEngine;

// JS interop for the WebGL build. Unity → window.parent via postMessage.
// The Next.js play page listens for { type: "momentum-quit", sessionId } and routes
// the user to /classement/[sessionId].
public static class WebBridge
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void MomentumPostQuit(string sessionId);
#endif

    public static void NotifyQuit(string sessionId)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        MomentumPostQuit(sessionId ?? "");
#else
        Debug.Log($"[WebBridge] (editor) NotifyQuit called with sessionId='{sessionId}'");
#endif
    }
}
#endif
