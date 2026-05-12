#if WEB_BUILD
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using UnityEngine;
using Colyseus;
using Colyseus.Schema;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    [Header("Server")]
    public string serverUrl = "ws://localhost:2567";

    public Colyseus.Client Client { get; private set; }
    public Colyseus.Room<GameState> Room { get; private set; }
    public StateCallbackStrategy<GameState> Callbacks { get; private set; }
    public string MySessionId => Room?.SessionId ?? "";
    public bool IsConnecting { get; private set; } = false;

    public event Action<string, PlayerState> OnPlayerAdded;
    public event Action<string> OnPlayerRemoved;
    public event Action OnConnected;
    public event Action<string> OnConnectionFailed;

    // Returned by Callbacks.OnAdd / OnRemove — call to unsubscribe
    private Action _removeOnAdd;
    private Action _removeOnRemove;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoBootstrap()
    {
        if (FindObjectOfType<NetworkManager>() != null) return;
        var go = new GameObject("NetworkManager (auto)");
        go.AddComponent<NetworkManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    async void Start()
    {
        if (!WebBootstrap.IsReady)
        {
            OnConnectionFailed?.Invoke("Missing sessionId or token in URL");
            return;
        }
        try
        {
            await Connect(WebBootstrap.SessionId, WebBootstrap.Token);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkManager] Unhandled exception in Start: {ex}");
            OnConnectionFailed?.Invoke(ex.Message);
        }
    }

    public async Task Connect(string sessionId, string token)
    {
        if (IsConnecting)
        {
            Debug.LogWarning("[NetworkManager] Connect called while already connecting — ignored");
            return;
        }
        IsConnecting = true;
        try
        {
            UnsubscribeRoomEvents();

            Client = new Colyseus.Client(serverUrl);
            var options = new Dictionary<string, object>
            {
                { "sessionId", sessionId },
                { "token", token },
            };
            Room = await Client.JoinOrCreate<GameState>("momentum", options);
            Debug.Log($"[NetworkManager] Joined room {Room.RoomId} as {Room.SessionId}");

            Callbacks = Colyseus.Schema.Callbacks.Get(Room);
            _removeOnAdd = Callbacks.OnAdd<PlayerState>(
                s => s.players,
                (sId, player) => OnPlayerAdded?.Invoke(sId, player));
            _removeOnRemove = Callbacks.OnRemove<PlayerState>(
                s => s.players,
                (sId, _) => OnPlayerRemoved?.Invoke(sId));

            OnConnected?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkManager] Connection failed: {ex.Message}");
            OnConnectionFailed?.Invoke(ex.Message);
        }
        finally
        {
            IsConnecting = false;
        }
    }

    private void UnsubscribeRoomEvents()
    {
        _removeOnAdd?.Invoke();
        _removeOnRemove?.Invoke();
        _removeOnAdd = null;
        _removeOnRemove = null;
    }

    public void SendInput(PlayerInputPayload payload) => Room?.Send("input", payload);
    public void SendStun() => Room?.Send("stun");
    public void SendFinish(float score) => Room?.Send("finish", new FinishPayload { score = score });

    void OnDestroy()
    {
        UnsubscribeRoomEvents();
        Room?.Leave();
    }
}

[Serializable]
public class PlayerInputPayload
{
    public float posX, posY, posZ;
    public float velX, velY, velZ;
    public float rotY;
    public bool isGrounded;
    public bool isSliding;
    public float horizontalInput;
}

[Serializable]
public class FinishPayload
{
    public float score;
}
#endif
