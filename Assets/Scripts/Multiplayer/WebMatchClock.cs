#if WEB_BUILD
using UnityEngine;

// Singleton local mirror of GameState.elapsedTime. The server ticks elapsedTime
// every 100ms (see momentum-server/src/rooms/MomentumRoom.startGame). To drive
// frame-rate-smooth animations from a shared server clock, we re-anchor on every
// observed server change and interpolate between ticks using local realtime.
//
// Both clients receive the same elapsedTime stream, so any feature reading
// WebMatchClock.MatchTime computes deterministically identical values, which is
// how we keep moving platforms (and any other purely time-driven object) in sync
// without broadcasting per-object state.
public class WebMatchClock : MonoBehaviour
{
    public static WebMatchClock Instance { get; private set; }

    private float _lastServerElapsed = -1f;
    private float _localAnchor;
    private bool _hasAnchor;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoBootstrap()
    {
        if (FindObjectOfType<WebMatchClock>() != null) return;
        var go = new GameObject("WebMatchClock (auto)");
        go.AddComponent<WebMatchClock>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        var nm = NetworkManager.Instance;
        if (nm == null || nm.Room == null || nm.Room.State == null) return;

        float serverElapsed = nm.Room.State.elapsedTime;
        if (Mathf.Abs(serverElapsed - _lastServerElapsed) > 0.001f)
        {
            _lastServerElapsed = serverElapsed;
            _localAnchor = Time.realtimeSinceStartup;
            _hasAnchor = true;
        }
    }

    public float MatchTime
    {
        get
        {
            if (!_hasAnchor) return 0f;
            return _lastServerElapsed + (Time.realtimeSinceStartup - _localAnchor);
        }
    }

    // True once the server has actually started counting (status="playing"). Lets
    // consumers keep their initial pose until the match really begins.
    public bool HasStarted => _hasAnchor && _lastServerElapsed > 0.0001f;
}
#endif
