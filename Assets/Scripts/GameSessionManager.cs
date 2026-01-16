using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;

/// <summary>
/// Gère la session de jeu avec l'API Next.js
/// Crée une session, récupère les QR codes et poll pour les pseudos
/// </summary>
public class GameSessionManager : MonoBehaviour
{
    public static GameSessionManager Instance { get; private set; }

    [Header("API Configuration")]
    [Tooltip("URL de base de l'API Next.js")]
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
        string url = $"{apiBaseUrl}/api/game/session";

        if (showDebug)
        {
            Debug.Log($"GameSession: Création de session via {url}");
        }

        using (UnityWebRequest request = UnityWebRequest.PostWwwForm(url, ""))
        {
            request.SetRequestHeader("Content-Type", "application/json");

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
        string url = $"{apiBaseUrl}/api/game/players?sessionId={sessionId}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
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
    }
}
