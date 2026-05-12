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

    public event Action<string, PlayerState> OnPlayerAdded;
    public event Action<string> OnPlayerRemoved;
    public event Action OnConnected;
    public event Action<string> OnConnectionFailed;

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
        await Connect(WebBootstrap.SessionId, WebBootstrap.Token);
    }

    public async Task Connect(string sessionId, string token)
    {
        try
        {
            Client = new ColyseusClient(serverUrl);
            var options = new Dictionary<string, object>
            {
                { "sessionId", sessionId },
                { "token", token },
            };
            Room = await Client.JoinOrCreate<GameState>("momentum", options);
            Debug.Log($"[NetworkManager] Joined room {Room.RoomId} as {Room.SessionId}");

            Room.State.players.OnAdd += (sId, player) => OnPlayerAdded?.Invoke(sId, player);
            Room.State.players.OnRemove += (sId, _) => OnPlayerRemoved?.Invoke(sId);

            OnConnected?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkManager] Connection failed: {ex.Message}");
            OnConnectionFailed?.Invoke(ex.Message);
        }
    }

    public void SendInput(PlayerInputPayload payload) => Room?.Send("input", payload);
    public void SendStun() => Room?.Send("stun");
    public void SendFinish(int score) => Room?.Send("finish", new FinishPayload { score = score });

    void OnDestroy()
    {
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
    public int score;
}
#endif
