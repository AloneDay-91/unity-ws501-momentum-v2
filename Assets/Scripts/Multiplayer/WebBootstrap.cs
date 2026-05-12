using UnityEngine;
using System.Runtime.InteropServices;

public class WebBootstrap : MonoBehaviour
{
    public static string SessionId { get; private set; } = "";
    public static string Token { get; private set; } = "";
    public static bool IsReady { get; private set; } = false;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern string GetUrlParam(string key);
#endif

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        ReadParams();
    }

    void Start()
    {
#if WEB_BUILD
        if (!IsReady)
        {
            Debug.LogError("[WebBootstrap] sessionId or token missing — cannot initialize web mode");
            return;
        }
        if (GameSessionManager.Instance != null)
        {
            GameSessionManager.Instance.InitWebMode();
        }
        else
        {
            Debug.LogError("[WebBootstrap] No GameSessionManager found in scene");
        }
#endif
    }

    private void ReadParams()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        SessionId = GetUrlParam("sessionId") ?? "";
        Token = GetUrlParam("token") ?? "";
#else
        // In Editor, read from PlayerPrefs to ease dev testing
        SessionId = PlayerPrefs.GetString("DEBUG_SESSION_ID", "TEST-ROOM");
        Token = PlayerPrefs.GetString("DEBUG_TOKEN", "tok-p1");
#endif
        IsReady = !string.IsNullOrEmpty(SessionId) && !string.IsNullOrEmpty(Token);
        Debug.Log($"[WebBootstrap] sessionId={SessionId}, hasToken={!string.IsNullOrEmpty(Token)}");
    }
}
