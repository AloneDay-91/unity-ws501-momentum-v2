using UnityEngine;
using UnityEngine.SceneManagement;
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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoBootstrap()
    {
        // Auto-create the WebBootstrap GameObject before any scene loads,
        // so that MenuPageManager.Start() can read WebBootstrap.IsReady reliably.
        if (FindObjectOfType<WebBootstrap>() != null) return;
        var go = new GameObject("WebBootstrap (auto)");
        go.AddComponent<WebBootstrap>();
    }

    void Awake()
    {
        // Singleton-ish: if a duplicate exists (e.g., manually placed in scene), keep the first
        if (FindObjectsOfType<WebBootstrap>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        ReadParams();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        // Initial scene case (in case the boot scene IS already the game scene)
        TryInitWebModeForCurrentScene();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryInitWebModeForCurrentScene();
    }

    private void TryInitWebModeForCurrentScene()
    {
#if WEB_BUILD
        if (!IsReady) return;
        var sceneName = SceneManager.GetActiveScene().name;
        var gsm = GameSessionManager.Instance ?? FindObjectOfType<GameSessionManager>();
        Debug.Log($"[DIAG][WebBootstrap] TryInitWebModeForCurrentScene at T={Time.realtimeSinceStartup:F3}s, scene='{sceneName}', gsm={(gsm != null ? "OK" : "NULL")}");
        if (gsm != null)
        {
            gsm.InitWebMode();
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
