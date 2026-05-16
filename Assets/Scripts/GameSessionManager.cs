using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;
using Anatidae;

/// <summary>
/// Gère la session de jeu avec l'API Next.js
/// Utilise AnatidaeProxyWebRequest pour contourner les erreurs CORS en WebGL
/// </summary>
public class GameSessionManager : MonoBehaviour
{
    public static GameSessionManager Instance { get; private set; }

    [Header("API Configuration")]
    [Tooltip("URL de base de l'API Next.js (VPS)")]
    public string apiBaseUrl = "http://localhost:3000";

    [Tooltip("Intervalle de polling en secondes")]
    public float pollingInterval = 5f;

    [Header("Debug")]
    public bool showDebug = true;

    // Session State (accessible en lecture seule)
    public string sessionId { get; private set; }
    public string player1Url { get; private set; }
    public string player2Url { get; private set; }
    public string player1QRCodeUrl { get; private set; }
    public string player2QRCodeUrl { get; private set; }
    public string player1Pseudo { get; private set; }
    public string player2Pseudo { get; private set; }
    public bool player1Ready { get; private set; }
    public bool player2Ready { get; private set; }
    public bool bothPlayersReady { get; private set; }

    // Événements
    public static Action OnSessionCreated;
    public static Action<string> OnPlayer1Joined;
    public static Action<string> OnPlayer2Joined;
    public static Action OnBothPlayersReady;

    private Coroutine pollingCoroutine;
    private bool isPolling = false;

    [Serializable]
    private class SessionResponse
    {
        public bool success;
        public string sessionId;
        public string player1Url;
        public string player2Url;
        public string player1QRCode;
        public string player2QRCode;
        public int expiresIn;
    }

    [Serializable]
    private class PlayersResponse
    {
        public bool success;
        public PlayerData player1;
        public PlayerData player2;
        public bool bothReady;
    }

    [Serializable]
    private class PlayerData
    {
        public string pseudo;
        public bool hasJoined;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Crée une nouvelle session de jeu
    /// </summary>
    public void CreateGameSession()
    {
#if WEB_BUILD
        // In iframe multiplayer the Next.js lobby created the session before Unity loaded;
        // its id arrived via URL param (WebBootstrap.SessionId → assigned in InitWebMode).
        // Calling /api/game/session here would create a parallel b45e07… row and overwrite
        // our sessionId — scores would then attach to that orphan session and never appear
        // at /classement/[the-lobby-code]. Just skip and reuse the inherited id.
        if (!string.IsNullOrEmpty(sessionId))
        {
            if (showDebug) Debug.Log($"[GameSessionManager] WEB_BUILD: CreateGameSession skipped — reusing inherited sessionId='{sessionId}'");
            OnSessionCreated?.Invoke();
            return;
        }
#endif
        StartCoroutine(CreateSessionCoroutine());
    }

    private IEnumerator CreateSessionCoroutine()
    {
        // Nettoie l'URL de base (enlève les espaces)
        string cleanBaseUrl = apiBaseUrl.Trim();
        string url = $"{cleanBaseUrl}/api/game/session";

        if (showDebug)
        {
            Debug.Log($"GameSession: Création de session via {url}");
        }

        // Utilise AnatidaeProxyWebRequest pour contourner CORS en WebGL
        using (UnityWebRequest request = AnatidaeProxyWebRequest.Post(url, "{}", "application/json"))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                SessionResponse response = JsonUtility.FromJson<SessionResponse>(json);

                if (response.success)
                {
                    sessionId = response.sessionId;
                    player1Url = response.player1Url;
                    player2Url = response.player2Url;
                    player1QRCodeUrl = response.player1QRCode;
                    player2QRCodeUrl = response.player2QRCode;

                    if (showDebug)
                    {
                        Debug.Log($"GameSession: Session créée - {sessionId}");
                        Debug.Log($"Player 1 URL: {player1Url}");
                        Debug.Log($"Player 2 URL: {player2Url}");
                        Debug.Log($"Player 1 QR: {player1QRCodeUrl}");
                        Debug.Log($"Player 2 QR: {player2QRCodeUrl}");
                    }

                    // Sauvegarde les URLs pour PlayerNameManager
                    if (PlayerNameManager.Instance != null)
                    {
                        PlayerNameManager.Instance.SetPlayer1Name("");
                        PlayerNameManager.Instance.SetPlayer2Name("");
                    }

                    OnSessionCreated?.Invoke();

                    // Démarre le polling
                    StartPolling();
                }
                else
                {
                    Debug.LogError("GameSession: Échec de création de session");
                }
            }
            else
            {
                Debug.LogError($"GameSession: Erreur réseau - {request.error}");
            }
        }
    }

    /// <summary>
    /// Démarre le polling pour récupérer les pseudos
    /// </summary>
    public void StartPolling()
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            Debug.LogError("GameSession: Impossible de démarrer le polling - pas de sessionId");
            return;
        }

        if (isPolling)
        {
            Debug.LogWarning("GameSession: Le polling est déjà actif");
            return;
        }

        isPolling = true;
        pollingCoroutine = StartCoroutine(PollPlayersCoroutine());

        if (showDebug)
        {
            Debug.Log("GameSession: Polling démarré");
        }
    }

    /// <summary>
    /// Arrête le polling
    /// </summary>
    public void StopPolling()
    {
        if (pollingCoroutine != null)
        {
            StopCoroutine(pollingCoroutine);
            pollingCoroutine = null;
        }
        isPolling = false;

        if (showDebug)
        {
            Debug.Log("GameSession: Polling arrêté");
        }
    }

    private IEnumerator PollPlayersCoroutine()
    {
        while (isPolling && !bothPlayersReady)
        {
            yield return FetchPlayersData();
            yield return new WaitForSeconds(pollingInterval);
        }
    }

    private IEnumerator FetchPlayersData()
    {
        string cleanBaseUrl = apiBaseUrl.Trim();
        string url = $"{cleanBaseUrl}/api/game/players?sessionId={sessionId}";

        // Utilise AnatidaeProxyWebRequest pour contourner CORS en WebGL
        using (UnityWebRequest request = AnatidaeProxyWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                PlayersResponse response = JsonUtility.FromJson<PlayersResponse>(json);

                if (response.success)
                {
                    // Joueur 1
                    bool player1Changed = false;
                    if (response.player1.hasJoined && !string.IsNullOrEmpty(response.player1.pseudo))
                    {
                        if (player1Pseudo != response.player1.pseudo)
                        {
                            player1Pseudo = response.player1.pseudo;
                            player1Ready = true;
                            player1Changed = true;

                            if (showDebug)
                            {
                                Debug.Log($"GameSession: Joueur 1 rejoint - {player1Pseudo}");
                            }

                            // Sauvegarde dans PlayerNameManager
                            if (PlayerNameManager.Instance != null)
                            {
                                PlayerNameManager.Instance.SetPlayer1Name(player1Pseudo);
                            }

                            OnPlayer1Joined?.Invoke(player1Pseudo);
                        }
                    }

                    // Joueur 2
                    bool player2Changed = false;
                    if (response.player2.hasJoined && !string.IsNullOrEmpty(response.player2.pseudo))
                    {
                        if (player2Pseudo != response.player2.pseudo)
                        {
                            player2Pseudo = response.player2.pseudo;
                            player2Ready = true;
                            player2Changed = true;

                            if (showDebug)
                            {
                                Debug.Log($"GameSession: Joueur 2 rejoint - {player2Pseudo}");
                            }

                            // Sauvegarde dans PlayerNameManager
                            if (PlayerNameManager.Instance != null)
                            {
                                PlayerNameManager.Instance.SetPlayer2Name(player2Pseudo);
                            }

                            OnPlayer2Joined?.Invoke(player2Pseudo);
                        }
                    }

                    // Vérifier si les deux sont prêts
                    if (response.bothReady && !bothPlayersReady)
                    {
                        bothPlayersReady = true;

                        if (showDebug)
                        {
                            Debug.Log("GameSession: Les deux joueurs sont prêts!");
                        }

                        OnBothPlayersReady?.Invoke();
                        StopPolling();
                    }
                }
            }
            else if (request.responseCode == 404 || request.responseCode == 410)
            {
                Debug.LogError("GameSession: Session expirée ou introuvable");
                StopPolling();
            }
            else
            {
                Debug.LogWarning($"GameSession: Erreur polling - {request.error}");
            }
        }
    }

    // Événement déclenché quand la partie démarre officiellement
    public static event System.Action OnGameStarted;

    // Événement déclenché quand la partie est relancée
    public static event System.Action OnGameRestarted;

    // WEB_BUILD: déclenché quand le serveur passe status="finished"
    public static event System.Action OnGameFinished;

    /// <summary>
    /// Démarre la partie en appelant /api/game/start
    /// À appeler quand les deux joueurs sont prêts et qu'on veut lancer le jeu
    /// </summary>
    public void StartGame(string mapName = "default", Action<bool> onComplete = null)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            Debug.LogError("GameSession: Impossible de démarrer - pas de sessionId");
            onComplete?.Invoke(false);
            return;
        }

        if (!bothPlayersReady)
        {
            Debug.LogWarning("GameSession: Les deux joueurs doivent être prêts pour démarrer");
            onComplete?.Invoke(false);
            return;
        }

        StartCoroutine(StartGameCoroutine(mapName, onComplete));
    }

    [System.Serializable]
    private class StartGameRequest
    {
        public string sessionId;
        public string mapName;
    }

    [System.Serializable]
    private class StartGameResponse
    {
        public bool success;
        public string message;
        public string error;
    }

    private IEnumerator StartGameCoroutine(string mapName, Action<bool> onComplete)
    {
        string cleanBaseUrl = apiBaseUrl.Trim();
        string url = $"{cleanBaseUrl}/api/game/start";

        StartGameRequest requestData = new StartGameRequest
        {
            sessionId = sessionId,
            mapName = mapName
        };

        string jsonData = JsonUtility.ToJson(requestData);

        if (showDebug)
        {
            Debug.Log($"GameSession: Démarrage de la partie via {url}");
        }

        // Utilise AnatidaeProxyWebRequest pour contourner CORS en WebGL
        using (UnityWebRequest request = AnatidaeProxyWebRequest.Post(url, jsonData, "application/json"))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                StartGameResponse response = JsonUtility.FromJson<StartGameResponse>(responseText);

                if (response.success)
                {
                    if (showDebug)
                    {
                        Debug.Log("GameSession: Partie démarrée avec succès!");
                    }

                    OnGameStarted?.Invoke();
                    onComplete?.Invoke(true);
                }
                else
                {
                    Debug.LogError($"GameSession: Échec du démarrage - {response.error}");
                    onComplete?.Invoke(false);
                }
            }
            else
            {
                Debug.LogError($"GameSession: Erreur réseau - {request.error}");
                Debug.LogError($"GameSession: Réponse - {request.downloadHandler.text}");
                onComplete?.Invoke(false);
            }
        }
    }

    /// <summary>
    /// Relance la partie sur la même session (garde les mêmes pseudos)
    /// À appeler quand on fait "Rejouer" après une partie terminée
    /// </summary>
    public void RestartGame(string mapName = "default", Action<bool> onComplete = null)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            Debug.LogError("GameSession: Impossible de relancer - pas de sessionId");
            onComplete?.Invoke(false);
            return;
        }

        StartCoroutine(RestartGameCoroutine(mapName, onComplete));
    }

    [System.Serializable]
    private class RestartGameRequest
    {
        public string sessionId;
        public string mapName;
    }

    [System.Serializable]
    private class RestartGameResponse
    {
        public bool success;
        public string message;
        public string error;
    }

    private IEnumerator RestartGameCoroutine(string mapName, Action<bool> onComplete)
    {
        string cleanBaseUrl = apiBaseUrl.Trim();
        string url = $"{cleanBaseUrl}/api/game/restart";

        RestartGameRequest requestData = new RestartGameRequest
        {
            sessionId = sessionId,
            mapName = mapName
        };

        string jsonData = JsonUtility.ToJson(requestData);

        if (showDebug)
        {
            Debug.Log($"GameSession: Relance de la partie via {url}");
        }

        using (UnityWebRequest request = AnatidaeProxyWebRequest.Post(url, jsonData, "application/json"))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                RestartGameResponse response = JsonUtility.FromJson<RestartGameResponse>(responseText);

                if (response.success)
                {
                    if (showDebug)
                    {
                        Debug.Log("GameSession: Partie relancée avec succès!");
                    }

                    OnGameRestarted?.Invoke();
                    onComplete?.Invoke(true);
                }
                else
                {
                    Debug.LogError($"GameSession: Échec de la relance - {response.error}");
                    onComplete?.Invoke(false);
                }
            }
            else
            {
                Debug.LogError($"GameSession: Erreur réseau - {request.error}");
                Debug.LogError($"GameSession: Réponse - {request.downloadHandler.text}");
                onComplete?.Invoke(false);
            }
        }
    }

    /// <summary>
    /// Classes pour sérialiser les données en JSON
    /// </summary>
    [System.Serializable]
    public class EndGameRequest
    {
        public string sessionId;
        public PlayerScoreData[] scores;
    }

    [System.Serializable]
    public class PlayerScoreData
    {
        public int playerNumber;
        public int totalScore;
        public float distanceTraveled;
        public float survivalTime;
        public int collectiblesCollected;  // Orbes de lumière (10 pts chacun)
        public int perfectJumps;           // Sauts parfaits (100 pts chacun)
        public bool hasFinished;           // Terminé le parcours (1000 pts bonus)
    }

    /// <summary>
    /// Envoie les scores à l'API. Persistant entre les scènes.
    /// </summary>
    public void SendScores(PlayerScoreData[] scores, Action<bool> onComplete = null)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            Debug.LogWarning("GameSession: Pas de sessionId, impossible de sauvegarder les scores");
            onComplete?.Invoke(false);
            return;
        }

        StartCoroutine(SendScoresCoroutine(scores, onComplete));
    }

    private IEnumerator SendScoresCoroutine(PlayerScoreData[] scores, Action<bool> onComplete)
    {
        string cleanBaseUrl = apiBaseUrl.Trim();
        string url = $"{cleanBaseUrl}/api/game/end";

        EndGameRequest request = new EndGameRequest
        {
            sessionId = sessionId,
            scores = scores
        };

        string jsonData = JsonUtility.ToJson(request);

        if (showDebug)
        {
            Debug.Log($"GameSession: Envoi des scores à {url}");
        }

        using (UnityWebRequest webRequest = AnatidaeProxyWebRequest.Post(url, jsonData, "application/json"))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                if (showDebug)
                {
                    Debug.Log($"GameSession: Scores sauvegardés avec succès!");
                }
                onComplete?.Invoke(true);
            }
            else
            {
                Debug.LogError($"GameSession: Erreur lors de la sauvegarde des scores - {webRequest.error}");
                Debug.LogError($"GameSession: Réponse: {webRequest.downloadHandler.text}");
                // On considère que ça a échoué, mais on ne bloque pas le jeu
                onComplete?.Invoke(false);
            }
        }
    }

#if WEB_BUILD
    // -------------------------------------------------------------------------
    // WEB_BUILD multiplayer — scene-objects approach
    // Players are pre-placed in the scene. P2 is disabled in web mode.
    // LocalPlayerSync is added to P1. Remote players are driven by NetworkPlayer.
    // -------------------------------------------------------------------------

    private Dictionary<string, GameObject> remoteNetworkPlayers = new Dictionary<string, GameObject>();

    /// <summary>
    /// Call this from the game scene's initialisation (e.g. a GameCycleManager or
    /// a scene-load callback) once players are known to be in the scene.
    /// In arcade mode this method is compiled away.
    /// </summary>
    private GameObject _localP1Go;
    private GameObject _localP2Go;
    private bool _localPlayerInitialized = false;
    private int _localPlayerNumber = 0;

    public int LocalPlayerNumber => _localPlayerNumber;

    public void InitWebMode()
    {
        Debug.Log($"[DIAG][GameSessionManager] InitWebMode called at T={Time.realtimeSinceStartup:F3}s in scene='{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}'. NetworkManager.Instance={(NetworkManager.Instance != null ? "OK" : "NULL")}, Room={(NetworkManager.Instance?.Room != null ? "OK" : "NULL")}, Room.State={(NetworkManager.Instance?.Room?.State != null ? "OK" : "NULL")}, players count={(NetworkManager.Instance?.Room?.State?.players != null ? NetworkManager.Instance.Room.State.players.Count.ToString() : "null")}, MySId='{NetworkManager.Instance?.MySessionId ?? "null"}'");

        // Inherit the Next.js DB sessionId from the iframe URL param — this is the same id
        // used by /classement/[sessionId], so scores POSTed via SendScores end up attached
        // to the session the player sees on the recap page. Without this, CreateGameSession
        // would create a separate b45e07… row and scores would be invisible at /classement/DV4GB3.
        if (!string.IsNullOrEmpty(WebBootstrap.SessionId))
        {
            sessionId = WebBootstrap.SessionId;
            if (showDebug) Debug.Log($"[GameSessionManager] WEB_BUILD: sessionId inherited from iframe URL = '{sessionId}'");
        }

        // Find both player GameObjects in the scene — keep references but DO NOT disable
        // either yet. Server-assigned playerNumber decides which slot is "us" (red P1 or blue P2),
        // and we only learn it once our PlayerState arrives via HandlePlayerStateAdded.
        PlayerInput[] allInputs = FindObjectsOfType<PlayerInput>(includeInactive: true);
        _localP1Go = null;
        _localP2Go = null;
        foreach (var pi in allInputs)
        {
            if (pi.playerID == 1) _localP1Go = pi.gameObject;
            else if (pi.playerID == 2) _localP2Go = pi.gameObject;
        }
        if (showDebug) Debug.Log($"[GameSessionManager] WEB_BUILD: Found P1={(_localP1Go != null ? _localP1Go.name : "null")}, P2={(_localP2Go != null ? _localP2Go.name : "null")} — waiting for server playerNumber before assigning slots");

        // Reset the "initialized" flag on each scene entry so SetupLocalPlayer re-runs
        // after a scene reload (LocalPlayerSync, camera viewport, P2-disable must re-apply).
        _localPlayerInitialized = false;

        // Mode solo dev hors-ligne : pas de serveur pour assigner un slot joueur.
        // On configure directement P1 comme joueur local et on saute tout le wiring réseau.
        if (DevSolo.Active)
        {
            if (showDebug) Debug.Log("[GameSessionManager] DevSolo — configuration P1 local, sans réseau");
            _localPlayerNumber = 1;
            SetupLocalPlayer(1, attachSync: false);
            return;
        }

        // If we already know our playerNumber from a previous InitWebMode call, apply it immediately.
        // Useful when Room state was populated before the main scene loaded.
        if (_localPlayerNumber > 0)
        {
            SetupLocalPlayer(_localPlayerNumber);
        }

        // Subscribe to remote player events. Idempotent: dedupe by -= then +=,
        // so repeated InitWebMode calls (scene reload, OnSceneLoaded) don't pile up handlers.
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnPlayerAdded -= HandlePlayerStateAdded;
            NetworkManager.Instance.OnPlayerAdded += HandlePlayerStateAdded;
            NetworkManager.Instance.OnPlayerAdded -= HandleRemotePlayerAdded;
            NetworkManager.Instance.OnPlayerAdded += HandleRemotePlayerAdded;
            NetworkManager.Instance.OnPlayerRemoved -= HandleRemotePlayerRemoved;
            NetworkManager.Instance.OnPlayerRemoved += HandleRemotePlayerRemoved;

            // Server-driven scene transition: when state.status flips to "playing",
            // every client fires OnGameStarted → LobbyPageUI loads "main". Both clients
            // transition on the same server tick, no manual sync required.
            NetworkManager.Instance.OnConnected -= SetupGameStateListener;
            NetworkManager.Instance.OnConnected += SetupGameStateListener;
            if (NetworkManager.Instance.Room != null) SetupGameStateListener();

            if (showDebug) Debug.Log("[GameSessionManager] WEB_BUILD: Subscribed to NetworkManager events");

            // Catch up: if Room already has players, replay them now (subscribing late shouldn't lose existing players)
            if (NetworkManager.Instance.Room != null && NetworkManager.Instance.Room.State != null && NetworkManager.Instance.Room.State.players != null)
            {
                int catchupCount = NetworkManager.Instance.Room.State.players.Count;
                Debug.Log($"[DIAG][GameSessionManager] Catch-up replay starting: {catchupCount} player(s) already in Room.State.players");
                NetworkManager.Instance.Room.State.players.ForEach((sId, ps) =>
                {
                    Debug.Log($"[DIAG][GameSessionManager] Catch-up: replaying sId='{sId}'");
                    HandlePlayerStateAdded(sId, ps);
                    HandleRemotePlayerAdded(sId, ps);
                });
            }
            else
            {
                Debug.Log($"[DIAG][GameSessionManager] Catch-up skipped — Room or State not ready yet. Will rely on OnPlayerAdded callback for future joins.");
            }
        }
        else
        {
            Debug.LogError("[GameSessionManager] WEB_BUILD: NetworkManager.Instance is null — make sure WebBootstrap+NetworkManager are in the scene before GameSessionManager runs");
        }
    }

    private string _previousServerStatus = "";
    private Colyseus.Room<GameState>.StateChangeEventHandler _stateChangeHandler;
    private Colyseus.Room<GameState> _stateChangeRoom;

    /// <summary>
    /// Subscribes to top-level Room.OnStateChange and fires OnGameStarted when state.status
    /// flips to "playing". Server triggers this transition automatically once both players
    /// join + 3s countdown ends, so both clients receive the change on the same server tick
    /// → scene loads in sync.
    /// Idempotent: removes previous handler before re-subscribing.
    /// </summary>
    private void SetupGameStateListener()
    {
        var nm = NetworkManager.Instance;
        if (nm?.Room?.State == null)
        {
            if (showDebug) Debug.Log("[GameSessionManager] WEB_BUILD: SetupGameStateListener skipped — Room/State not ready");
            return;
        }

        // Detach from previous Room if any (defensive — Connect could in theory re-create the Room)
        if (_stateChangeRoom != null && _stateChangeHandler != null)
        {
            _stateChangeRoom.OnStateChange -= _stateChangeHandler;
        }

        _stateChangeRoom = nm.Room;
        _previousServerStatus = nm.Room.State.status ?? "";

        _stateChangeHandler = (state, isFirstState) =>
        {
            var currentStatus = state.status ?? "";
            if (currentStatus == _previousServerStatus) return;

            if (showDebug) Debug.Log($"[GameSessionManager] WEB_BUILD: state.status '{_previousServerStatus}' → '{currentStatus}' (countdown={state.countdownRemaining}, isFirstState={isFirstState})");
            _previousServerStatus = currentStatus;

            // "loading": server says both players joined → every client must LoadScene("main").
            // Once each client's scene is up it sends "sceneReady" and the server proceeds to
            // status="countdown" (visible in-game) and then "playing". This handshake keeps the
            // in-game countdown perfectly aligned across clients even when WebGL load times diverge.
            if (currentStatus == "loading")
            {
                if (showDebug) Debug.Log("[GameSessionManager] WEB_BUILD: status=loading → firing OnGameStarted (LobbyPageUI will LoadScene main)");
                OnGameStarted?.Invoke();
            }
            else if (currentStatus == "finished")
            {
                if (showDebug) Debug.Log("[GameSessionManager] WEB_BUILD: status=finished → firing OnGameFinished");
                OnGameFinished?.Invoke();
            }
        };

        _stateChangeRoom.OnStateChange += _stateChangeHandler;

        if (showDebug) Debug.Log($"[GameSessionManager] WEB_BUILD: Room.OnStateChange listener installed, current status='{_previousServerStatus}'");
    }

    /// <summary>
    /// Populates player1Pseudo / player2Pseudo / ready flags from the PlayerState sent by the server.
    /// Fires for ALL players (self and remote) so the lobby UI knows both names.
    /// </summary>
    private void HandlePlayerStateAdded(string addedSessionId, PlayerState state)
    {
        if (state == null) return;
        bool changed = false;

        if (state.playerNumber == 1)
        {
            if (player1Pseudo != state.pseudo)
            {
                player1Pseudo = state.pseudo;
                changed = true;
            }
            if (!player1Ready) { player1Ready = true; changed = true; }
            if (changed) OnPlayer1Joined?.Invoke(player1Pseudo);
        }
        else if (state.playerNumber == 2)
        {
            if (player2Pseudo != state.pseudo)
            {
                player2Pseudo = state.pseudo;
                changed = true;
            }
            if (!player2Ready) { player2Ready = true; changed = true; }
            if (changed) OnPlayer2Joined?.Invoke(player2Pseudo);
        }

        if (player1Ready && player2Ready && !bothPlayersReady)
        {
            bothPlayersReady = true;
            OnBothPlayersReady?.Invoke();
            if (showDebug) Debug.Log("[GameSessionManager] WEB_BUILD: Both players ready");
        }

        if (showDebug && changed)
        {
            Debug.Log($"[GameSessionManager] WEB_BUILD: PlayerState P{state.playerNumber} pseudo='{state.pseudo}' sId='{addedSessionId}'");
        }

        // Set up the local player when our own PlayerState arrives. We learn our slot
        // (P1 = red, P2 = blue) only from the server — the local PlayerInput.playerID
        // is just the scene-prepped slot, not a network identity.
        // Note: state.playerNumber is float (Colyseus schema "number" → C# float), cast to int.
        int statePlayerNumber = (int)state.playerNumber;
        if (NetworkManager.Instance != null
            && addedSessionId == NetworkManager.Instance.MySessionId
            && statePlayerNumber > 0)
        {
            _localPlayerNumber = statePlayerNumber;
            if (!_localPlayerInitialized) SetupLocalPlayer(statePlayerNumber);
        }
    }

    /// <summary>
    /// Apply the server-assigned player slot to the current scene: enable our GameObject
    /// (P1 or P2), disable the other one, attach LocalPlayerSync to ours, and switch
    /// the camera viewport to fullscreen for WebGL (no split-screen in single-local-player mode).
    /// </summary>
    private void SetupLocalPlayer(int playerNumber, bool attachSync = true)
    {
        if (_localP1Go == null && _localP2Go == null)
        {
            if (showDebug) Debug.Log("[GameSessionManager] WEB_BUILD: SetupLocalPlayer skipped — no player GameObjects in this scene (probably MainMenu)");
            return;
        }

        GameObject mine = playerNumber == 1 ? _localP1Go : _localP2Go;
        GameObject other = playerNumber == 1 ? _localP2Go : _localP1Go;

        if (other != null)
        {
            other.SetActive(false);
            if (showDebug) Debug.Log($"[GameSessionManager] WEB_BUILD: Disabled P{(playerNumber == 1 ? 2 : 1)} (remote slot — will be cloned for the other player)");
        }

        if (mine == null)
        {
            Debug.LogError($"[GameSessionManager] WEB_BUILD: SetupLocalPlayer — could not find P{playerNumber} GameObject in scene");
            return;
        }

        // Ensure the local slot is active (in case a prior pass disabled it)
        if (!mine.activeSelf) mine.SetActive(true);

        if (attachSync && mine.GetComponent<LocalPlayerSync>() == null)
            mine.AddComponent<LocalPlayerSync>();

        // Fullscreen camera: arcade mode uses split (P1 top, P2 bottom). In WebGL there's only
        // one local player, so the visible camera takes the whole viewport.
        foreach (var cam in mine.GetComponentsInChildren<Camera>(includeInactive: true))
        {
            cam.rect = new Rect(0f, 0f, 1f, 1f);
        }

        _localPlayerInitialized = true;
        if (showDebug) Debug.Log($"[GameSessionManager] WEB_BUILD: SetupLocalPlayer done — local is P{playerNumber}, LocalPlayerSync attached, camera fullscreen");
    }

    private void HandleRemotePlayerAdded(string remoteSessionId, PlayerState state)
    {
        Debug.Log($"[DIAG][GameSessionManager] HandleRemotePlayerAdded called at T={Time.realtimeSinceStartup:F3}s for sId='{remoteSessionId}', MySId='{NetworkManager.Instance?.MySessionId ?? "null"}', scene='{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}'");

        // Skip if this is our own session
        if (NetworkManager.Instance != null && remoteSessionId == NetworkManager.Instance.MySessionId)
        {
            Debug.Log($"[DIAG][GameSessionManager] -> Skipping (is own session)");
            return;
        }

        if (remoteNetworkPlayers.ContainsKey(remoteSessionId))
        {
            Debug.Log($"[DIAG][GameSessionManager] -> Skipping (already tracked)");
            if (showDebug) Debug.LogWarning($"[GameSessionManager] WEB_BUILD: Remote player {remoteSessionId} already tracked — skipping");
            return;
        }

        // Choose the clone source matching the remote player's server-assigned slot:
        // remote is P1 → clone P1 GameObject (red), remote is P2 → clone P2 (blue).
        // The matching scene slot is disabled (by SetupLocalPlayer) once we know our own
        // playerNumber, but Instantiate works on inactive source GameObjects too.
        GameObject sourceGo = null;
        if (state.playerNumber == 1) sourceGo = _localP1Go;
        else if (state.playerNumber == 2) sourceGo = _localP2Go;

        if (sourceGo == null)
        {
            // Fallback: scan the scene (handles edge case where InitWebMode wasn't called yet)
            PlayerInput[] allInputs = FindObjectsOfType<PlayerInput>(includeInactive: true);
            foreach (var pi in allInputs)
            {
                if (pi.playerID == state.playerNumber) { sourceGo = pi.gameObject; break; }
            }
        }

        if (sourceGo == null)
        {
            Debug.LogError($"[GameSessionManager] WEB_BUILD: Cannot find P{state.playerNumber} source GameObject to clone for remote player");
            return;
        }

        Vector3 spawnPos = sourceGo.transform.position;
        var go = Instantiate(sourceGo, spawnPos, sourceGo.transform.rotation);
        go.name = $"RemotePlayer_P{state.playerNumber}_{remoteSessionId}";

        // Suppress any Update() ticks until Bind() has neutralized the clone's components
        go.SetActive(false);

        // Disable cloned cameras and audio listeners — the remote player must not render its own viewport
        foreach (var cam in go.GetComponentsInChildren<Camera>(includeInactive: true))
        {
            cam.enabled = false;
        }
        foreach (var listener in go.GetComponentsInChildren<AudioListener>(includeInactive: true))
        {
            listener.enabled = false;
        }

        // Remove LocalPlayerSync if it was copied along
        var lps = go.GetComponent<LocalPlayerSync>();
        if (lps != null) Destroy(lps);

        var netPlayer = go.AddComponent<NetworkPlayer>();
        netPlayer.Bind(state);

        // Resume update lifecycle now that the clone is fully configured
        go.SetActive(true);

        remoteNetworkPlayers[remoteSessionId] = go;
        if (showDebug) Debug.Log($"[GameSessionManager] WEB_BUILD: Remote player spawned for session {remoteSessionId}");
    }

    private void HandleRemotePlayerRemoved(string remoteSessionId)
    {
        if (remoteNetworkPlayers.TryGetValue(remoteSessionId, out var go))
        {
            Destroy(go);
            remoteNetworkPlayers.Remove(remoteSessionId);
            if (showDebug) Debug.Log($"[GameSessionManager] WEB_BUILD: Remote player removed for session {remoteSessionId}");
        }
    }
#endif

    /// <summary>
    /// Réinitialise la session
    /// </summary>
    public void ResetSession()
    {
        StopPolling();
        sessionId = null;
        player1Url = null;
        player2Url = null;
        player1QRCodeUrl = null;
        player2QRCodeUrl = null;
        player1Pseudo = null;
        player2Pseudo = null;
        player1Ready = false;
        player2Ready = false;
        bothPlayersReady = false;

        if (showDebug)
        {
            Debug.Log("GameSession: Session réinitialisée");
        }
    }

    void OnDestroy()
    {
        StopPolling();
#if WEB_BUILD
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnPlayerAdded -= HandleRemotePlayerAdded;
            NetworkManager.Instance.OnPlayerRemoved -= HandleRemotePlayerRemoved;
        }
#endif
    }
}
