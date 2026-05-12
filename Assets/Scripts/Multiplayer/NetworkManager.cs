#if WEB_BUILD
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Colyseus;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    [Header("Server")]
    public string serverUrl = "ws://localhost:2567";

    public ColyseusClient Client { get; private set; }
    public ColyseusRoom<GameState> Room { get; private set; }
    public string MySessionId => Room?.SessionId ?? "";
    public bool IsConnecting { get; private set; } = false;

    public event Action<string, PlayerState> OnPlayerAdded;
    public event Action<string> OnPlayerRemoved;
    public event Action OnConnected;
    public event Action<string> OnConnectionFailed;

    // Store delegates so we can unsubscribe
    private Action<string, PlayerState> _onAddHandler;
    private Action<string, PlayerState> _onRemoveHandler;

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
            // Cleanup any prior subscriptions
            UnsubscribeRoomEvents();

            Client = new ColyseusClient(serverUrl);
            var options = new Dictionary<string, object>
            {
                { "sessionId", sessionId },
                { "token", token },
            };
            Room = await Client.JoinOrCreate<GameState>("momentum", options);
            Debug.Log($"[NetworkManager] Joined room {Room.RoomId} as {Room.SessionId}");

            _onAddHandler = (sId, player) => OnPlayerAdded?.Invoke(sId, player);
            _onRemoveHandler = (sId, _) => OnPlayerRemoved?.Invoke(sId);
            Room.State.players.OnAdd += _onAddHandler;
            Room.State.players.OnRemove += _onRemoveHandler;

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
        if (Room?.State?.players != null)
        {
            if (_onAddHandler != null) Room.State.players.OnAdd -= _onAddHandler;
            if (_onRemoveHandler != null) Room.State.players.OnRemove -= _onRemoveHandler;
        }
        _onAddHandler = null;
        _onRemoveHandler = null;
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
