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
    public void InitWebMode()
    {
        // Find local players by tag (tagged "Player") or by PlayerInput component
        PlayerInput[] allInputs = FindObjectsOfType<PlayerInput>();
        GameObject p1Go = null;
        GameObject p2Go = null;

        foreach (var pi in allInputs)
        {
            if (pi.playerID == 1) p1Go = pi.gameObject;
            else if (pi.playerID == 2) p2Go = pi.gameObject;
        }

        // Disable P2 — only one local player in web mode
        if (p2Go != null)
        {
            p2Go.SetActive(false);
            if (showDebug) Debug.Log("[GameSessionManager] WEB_BUILD: P2 disabled");
        }
        else
        {
            if (showDebug) Debug.Log("[GameSessionManager] WEB_BUILD: No P2 found in scene — nothing to disable");
        }

        // Add LocalPlayerSync to P1
        if (p1Go != null)
        {
            if (p1Go.GetComponent<LocalPlayerSync>() == null)
                p1Go.AddComponent<LocalPlayerSync>();
            if (showDebug) Debug.Log("[GameSessionManager] WEB_BUILD: LocalPlayerSync attached to P1");
        }
        else
        {
            Debug.LogError("[GameSessionManager] WEB_BUILD: Could not find P1 (playerID == 1) in scene");
        }

        // Subscribe to remote player events
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnPlayerAdded += HandleRemotePlayerAdded;
            NetworkManager.Instance.OnPlayerRemoved += HandleRemotePlayerRemoved;
            if (showDebug) Debug.Log("[GameSessionManager] WEB_BUILD: Subscribed to NetworkManager events");
        }
        else
        {
            Debug.LogError("[GameSessionManager] WEB_BUILD: NetworkManager.Instance is null — make sure WebBootstrap+NetworkManager are in the scene before GameSessionManager runs");
        }
    }

    private void HandleRemotePlayerAdded(string remoteSessionId, PlayerState state)
    {
        // Skip if this is our own session
        if (NetworkManager.Instance != null && remoteSessionId == NetworkManager.Instance.MySessionId)
            return;

        if (remoteNetworkPlayers.ContainsKey(remoteSessionId))
        {
            if (showDebug) Debug.LogWarning($"[GameSessionManager] WEB_BUILD: Remote player {remoteSessionId} already tracked — skipping");
            return;
        }

        // Spawn a copy of P1 to use as the remote-player puppet
        PlayerInput[] allInputs = FindObjectsOfType<PlayerInput>();
        GameObject p1Go = null;
        foreach (var pi in allInputs)
        {
            if (pi.playerID == 1) { p1Go = pi.gameObject; break; }
        }

        if (p1Go == null)
        {
            Debug.LogError("[GameSessionManager] WEB_BUILD: Cannot find P1 to clone for remote player");
            return;
        }

        // Determine spawn position: playerNumber == 2 → use P1 position offset; else same spot
        Vector3 spawnPos = p1Go.transform.position;
        var go = Instantiate(p1Go, spawnPos, p1Go.transform.rotation);
        go.name = $"RemotePlayer_{remoteSessionId}";

        // Remove LocalPlayerSync if it was copied along
        var lps = go.GetComponent<LocalPlayerSync>();
        if (lps != null) Destroy(lps);

        var netPlayer = go.AddComponent<NetworkPlayer>();
        netPlayer.Bind(state);

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
