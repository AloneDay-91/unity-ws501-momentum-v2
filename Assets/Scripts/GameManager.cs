using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class GameManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    public static GameManager Instance { get; private set; }

    [Header("UI Global")]
    public GameObject countdownOverlay;
    public GameObject gameOverPanel;

    [Header("UI Textes du GameOverPanel")]
    [Tooltip("Premier bouton à sélectionner lors du GameOver (ex: Rejouer)")]
    public GameObject gameOverFirstButton;

    [Tooltip("Texte principal (ex: 'Partie Terminée')")]
    public TMP_Text gameOverTitleText;
    [Tooltip("Texte du gagnant")]
    public TMP_Text winnerText;
    [Tooltip("Texte des scores")]
    public TMP_Text scoresText;

    [Header("Overlays individuels par joueur")]
    [Tooltip("Overlay affiché quand le joueur 1 est éliminé (dans son viewport)")]
    public GameObject player1EliminatedOverlay;
    [Tooltip("Overlay affiché quand le joueur 2 est éliminé (dans son viewport)")]
    public GameObject player2EliminatedOverlay;
    [Tooltip("Texte de l'overlay joueur 1")]
    public TMP_Text player1EliminatedText;
    [Tooltip("Texte de l'overlay joueur 2")]
    public TMP_Text player2EliminatedText;

    [Header("Game Settings")]
    public int countdownDuration = 3;

    [Header("Debug")]
    public bool showDebug = true;

    // État des joueurs
    private Dictionary<int, bool> playerGameOver = new Dictionary<int, bool>();
    private Dictionary<int, int> playerScores = new Dictionary<int, int>();
    private Dictionary<int, string> playerNames = new Dictionary<int, string>();
    private int totalPlayers = 0;
    private int playersFinished = 0;

    // API Integration
    private GameAPIClient apiClient;
    private AuthManager authManager;
    private float gameStartTime;
    public bool gameInProgress { get; private set; } = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        Time.timeScale = 1f;
        Physics.autoSimulation = true; // S'assure que la physique est active au démarrage

        // Initialize API components
        authManager = FindObjectOfType<AuthManager>();
        apiClient = FindObjectOfType<GameAPIClient>();

        // Cache les overlays au départ
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        if (player1EliminatedOverlay != null)
        {
            player1EliminatedOverlay.SetActive(false);
        }
        if (player2EliminatedOverlay != null)
        {
            player2EliminatedOverlay.SetActive(false);
        }

        // Initialise les joueurs
        InitializePlayers();

#if WEB_BUILD
        // Multiplayer: countdown is driven by the server (state.status: loading → countdown → playing).
        // The local 3s timer would race with the other client; we follow the server clock instead.
        GameSessionManager.OnGameFinished -= HandleServerGameFinished;
        GameSessionManager.OnGameFinished += HandleServerGameFinished;
        StartCoroutine(ServerDrivenCountdownCoroutine());
#else
        StartCoroutine(StartCountdownCoroutine());
#endif
    }

#if WEB_BUILD
    void OnDestroy()
    {
        GameSessionManager.OnGameFinished -= HandleServerGameFinished;
    }

    private void HandleServerGameFinished()
    {
        // Idempotent: ShowFinalGameOver re-activates the panel even if a previous local-death
        // path already did so. We don't gate on gameInProgress — if the local path silently
        // failed (panel hidden, ref null in some prefab override), this is the safety net.
        gameInProgress = false;
        if (showDebug) Debug.Log("GameManager: server says match finished → ShowFinalGameOver");
        ShowFinalGameOver();
    }

    private IEnumerator ServerDrivenCountdownCoroutine()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.SendSceneReady();
            if (showDebug) Debug.Log("GameManager: sent sceneReady to server");
        }

        PlayerInput[] allPlayerInputs = FindObjectsOfType<PlayerInput>();
        foreach (PlayerInput input in allPlayerInputs)
        {
            if (input.GetComponent<NetworkPlayer>() != null) continue;
            input.enabled = false;
        }

        // Dedicated fullscreen overlay so the player who joined first sees a black screen
        // (instead of the empty arena) while waiting for the opponent's scene to load.
        var waitOverlay = WebWaitOverlay.CreateAndShow();

        TextMeshProUGUI countdownText = null;
        if (countdownOverlay != null)
        {
            countdownOverlay.SetActive(true);
            countdownText = countdownOverlay.GetComponentInChildren<TextMeshProUGUI>();
        }

        var nm = NetworkManager.Instance;
        bool sawGo = false;
        while (true)
        {
            var state = nm?.Room?.State;
            if (state == null) { yield return null; continue; }

            string status = state.status ?? "";
            int countdown = Mathf.CeilToInt((float)state.countdownRemaining);

            // While waiting for the other client to load, hide the countdown number,
            // show the "waiting" overlay (opaque black + message), and keep inputs off.
            bool waiting = status == "loading" || (status == "countdown" && countdown <= 0);
            if (waitOverlay != null) waitOverlay.SetVisible(waiting);
            if (countdownText != null) countdownText.gameObject.SetActive(!waiting);

            if (countdownText != null && !waiting)
            {
                if (status == "playing") countdownText.text = "GO!";
                else if (status == "countdown" && countdown > 0) countdownText.text = countdown.ToString();
            }

            if (status == "playing")
            {
                if (!sawGo) { sawGo = true; yield return new WaitForSeconds(0.5f); }
                break;
            }

            yield return null;
        }

        if (countdownOverlay != null) countdownOverlay.SetActive(false);
        if (waitOverlay != null) waitOverlay.Destroy();

        gameStartTime = Time.time;
        gameInProgress = true;

        foreach (PlayerInput input in allPlayerInputs)
        {
            if (input.GetComponent<NetworkPlayer>() != null) continue;
            input.enabled = true;
        }

        foreach (PlayerTimer timer in FindObjectsOfType<PlayerTimer>()) timer.StartTimer();
        foreach (PlayerScoreTracker tracker in FindObjectsOfType<PlayerScoreTracker>()) tracker.StartTracking();

        if (showDebug) Debug.Log("GameManager: server-driven countdown done → game in progress");
    }
#endif

    /// <summary>
    /// Initialise la liste des joueurs
    /// </summary>
    private void InitializePlayers()
    {
        PlayerInput[] allPlayers = FindObjectsOfType<PlayerInput>();
        totalPlayers = allPlayers.Length;

        foreach (PlayerInput player in allPlayers)
        {
            int playerID = player.playerID;
            playerGameOver[playerID] = false;
            playerScores[playerID] = 0;

            // Récupère le nom depuis PlayerNameManager
            string playerName = $"Joueur {playerID}";
            if (PlayerNameManager.Instance != null)
            {
                playerName = PlayerNameManager.Instance.GetPlayerName(playerID);
                Debug.Log($"GameManager: Pseudo récupéré pour Joueur {playerID}: '{playerName}'");
            }
            else
            {
                Debug.LogWarning("GameManager: PlayerNameManager.Instance est null!");
            }
            playerNames[playerID] = playerName;

            // Enregistre dans le ScoreManager
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.RegisterPlayer(playerID);
            }

            if (showDebug)
            {
                Debug.Log($"GameManager: Joueur {playerID} initialisé - {playerName}");
            }
        }

        if (showDebug)
        {
            Debug.Log($"GameManager: {totalPlayers} joueurs initialisés");
        }
    }

    /// <summary>
    /// Rafraîchit les pseudos des joueurs depuis PlayerNameManager ou GameSessionManager
    /// Appelé juste avant d'afficher le GameOverPanel pour avoir les pseudos les plus récents
    /// </summary>
    private void RefreshPlayerNames()
    {
        if (showDebug)
        {
            Debug.Log("GameManager: Rafraîchissement des pseudos...");
        }

        // Tente de récupérer depuis GameSessionManager en priorité (données API)
        if (GameSessionManager.Instance != null)
        {
            string p1Pseudo = GameSessionManager.Instance.player1Pseudo;
            string p2Pseudo = GameSessionManager.Instance.player2Pseudo;

            if (!string.IsNullOrEmpty(p1Pseudo))
            {
                playerNames[1] = p1Pseudo;
                if (showDebug)
                {
                    Debug.Log($"GameManager: Pseudo Joueur 1 depuis GameSessionManager: '{p1Pseudo}'");
                }
            }

            if (!string.IsNullOrEmpty(p2Pseudo))
            {
                playerNames[2] = p2Pseudo;
                if (showDebug)
                {
                    Debug.Log($"GameManager: Pseudo Joueur 2 depuis GameSessionManager: '{p2Pseudo}'");
                }
            }
        }

        // Fallback: récupère depuis PlayerNameManager
        if (PlayerNameManager.Instance != null)
        {
            foreach (var playerID in playerNames.Keys)
            {
                string updatedName = PlayerNameManager.Instance.GetPlayerName(playerID);
                // Met à jour seulement si le nom n'est pas "Player X" par défaut
                if (!updatedName.StartsWith("Player ") && !updatedName.StartsWith("Joueur "))
                {
                    playerNames[playerID] = updatedName;
                    if (showDebug)
                    {
                        Debug.Log($"GameManager: Pseudo Joueur {playerID} depuis PlayerNameManager: '{updatedName}'");
                    }
                }
            }
        }
    }

    IEnumerator StartCountdownCoroutine()
    {
        // --- PREPARE FOR COUNTDOWN ---
        // Disable player controls
        PlayerInput[] allPlayerInputs = FindObjectsOfType<PlayerInput>();
        foreach (PlayerInput input in allPlayerInputs)
        {
            input.enabled = false;
        }

        // Activate the countdown UI and find the text component
        TextMeshProUGUI countdownText = null;
        if (countdownOverlay != null)
        {
            countdownOverlay.SetActive(true);
            countdownText = countdownOverlay.GetComponentInChildren<TextMeshProUGUI>();
        }

        // --- COUNTDOWN ---
        for (int i = countdownDuration; i > 0; i--)
        {
            if (countdownText != null)
            {
                countdownText.text = i.ToString();
            }
            yield return new WaitForSeconds(1f);
        }

        // --- GO! ---
        if (countdownText != null)
        {
            countdownText.text = "GO!";
        }
        yield return new WaitForSeconds(1f);

        // Deactivate the entire countdown overlay
        if (countdownOverlay != null)
        {
            countdownOverlay.SetActive(false);
        }

        // --- START THE GAME ---
        gameStartTime = Time.time;
        gameInProgress = true;

        // Enable player controls
        foreach (PlayerInput input in allPlayerInputs)
        {
            input.enabled = true;
        }

        // Start timers
        PlayerTimer[] allPlayerTimers = FindObjectsOfType<PlayerTimer>();
        foreach (PlayerTimer timer in allPlayerTimers)
        {
            timer.StartTimer();
        }

        // Start score tracking
        PlayerScoreTracker[] allScoreTrackers = FindObjectsOfType<PlayerScoreTracker>();
        foreach (PlayerScoreTracker tracker in allScoreTrackers)
        {
            tracker.StartTracking();
        }
    }

    /// <summary>
    /// Appelé quand un joueur est éliminé (touché par le laser, tombé, etc.)
    /// NE bloque PAS le jeu pour les autres joueurs
    /// </summary>
    public void OnPlayerEliminated(int playerID, int score)
    {
        if (!gameInProgress) return;
        if (playerGameOver.ContainsKey(playerID) && playerGameOver[playerID]) return;

#if WEB_BUILD
        // Tell the server only if we just killed OURSELVES locally. Remote deaths arrive
        // via state.isAlive callback (handled in NetworkPlayer) which calls this method
        // for the remote playerID — re-broadcasting would be wrong and cause loops.
        if (GameSessionManager.Instance != null
            && GameSessionManager.Instance.LocalPlayerNumber == playerID
            && NetworkManager.Instance != null)
        {
            NetworkManager.Instance.SendDeath();
        }
#endif

        playerGameOver[playerID] = true;
        playerScores[playerID] = score;
        playersFinished++;

        string playerName = playerNames.ContainsKey(playerID) ? playerNames[playerID] : $"Joueur {playerID}";

        if (showDebug)
        {
            Debug.Log($"GameManager: {playerName} éliminé! Score: {score}");
        }

        // Notifie le ScoreManager
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnPlayerEliminated(playerID);
        }

        // Affiche l'overlay individuel pour ce joueur (no-op en WEB_BUILD)
        ShowPlayerEliminatedOverlay(playerID, playerName, score);

        // Remplace le score live du joueur éliminé par une croix
        foreach (var ui in FindObjectsOfType<PlayerScoreUI>(includeInactive: true))
        {
            if (ui.playerID == playerID) ui.MarkEliminated();
        }

#if WEB_BUILD
        // As soon as the LOCAL player is out, surface the fullscreen game over panel with
        // Restart/Quit buttons. We don't wait for the opponent — the local player needs
        // immediate access to "Quitter" to leave the session.
        if (GameSessionManager.Instance != null
            && GameSessionManager.Instance.LocalPlayerNumber == playerID)
        {
            ShowFinalGameOver();
        }
#endif

        // Met à jour les AudioListeners car le joueur (et sa caméra) pourrait être désactivé
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ManageAudioListeners();
        }

        // Vérifie si tous les joueurs ont terminé
        CheckAllPlayersFinished();
    }

    /// <summary>
    /// Appelé quand un joueur termine le parcours (victoire)
    /// </summary>
    public void OnPlayerFinished(int playerID, int score)
    {
        if (!gameInProgress) return;
        if (playerGameOver.ContainsKey(playerID) && playerGameOver[playerID]) return;

        playerGameOver[playerID] = true;
        playerScores[playerID] = score;
        playersFinished++;

        string playerName = playerNames.ContainsKey(playerID) ? playerNames[playerID] : $"Joueur {playerID}";

        if (showDebug)
        {
            Debug.Log($"GameManager: {playerName} a terminé! Score: {score}");
        }

        // Notifie le ScoreManager
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnPlayerFinished(playerID);
        }

        // Affiche l'overlay individuel pour ce joueur
        ShowPlayerFinishedOverlay(playerID, playerName, score);

        // Vérifie si tous les joueurs ont terminé
        CheckAllPlayersFinished();
    }

    /// <summary>
    /// Affiche l'overlay "Éliminé" pour un joueur spécifique
    /// </summary>
    private void ShowPlayerEliminatedOverlay(int playerID, string playerName, int score)
    {
#if WEB_BUILD
        // In multiplayer we don't show the per-viewport elimination strip — the fullscreen
        // game over panel (with Restart/Quit buttons) is the single end-of-match UI.
        return;
#else
        GameObject overlay = null;
        TMP_Text overlayText = null;

        if (playerID == 1)
        {
            overlay = player1EliminatedOverlay;
            overlayText = player1EliminatedText;
        }
        else if (playerID == 2)
        {
            overlay = player2EliminatedOverlay;
            overlayText = player2EliminatedText;
        }

        if (overlay != null)
        {
            overlay.SetActive(true);
        }

        if (overlayText != null)
        {
            overlayText.text = $"ÉLIMINÉ!\n{playerName}\nScore: {score}";
        }

        if (showDebug)
        {
            Debug.Log($"GameManager: Overlay éliminé affiché pour joueur {playerID}");
        }
#endif
    }

    /// <summary>
    /// Affiche l'overlay "Terminé" pour un joueur qui a fini le parcours
    /// </summary>
    private void ShowPlayerFinishedOverlay(int playerID, string playerName, int score)
    {
        GameObject overlay = null;
        TMP_Text overlayText = null;

        if (playerID == 1)
        {
            overlay = player1EliminatedOverlay;
            overlayText = player1EliminatedText;
        }
        else if (playerID == 2)
        {
            overlay = player2EliminatedOverlay;
            overlayText = player2EliminatedText;
        }

        if (overlay != null)
        {
            overlay.SetActive(true);
        }

        if (overlayText != null)
        {
            overlayText.text = $"TERMINÉ!\n{playerName}\nScore: {score}";
        }

        if (showDebug)
        {
            Debug.Log($"GameManager: Overlay terminé affiché pour joueur {playerID}");
        }
    }

    /// <summary>
    /// Vérifie si tous les joueurs ont terminé et affiche le GameOverPanel final
    /// </summary>
    private void CheckAllPlayersFinished()
    {
#if WEB_BUILD
        // Client-side fallback so the panel shows even if the server status flip is delayed
        // or the OnStateChange handler isn't firing for some reason. Count from the live
        // Colyseus room state — both players must have isAlive==false OR hasFinished==true.
        int alivePlayers = 0;
        int knownPlayers = 0;
        if (NetworkManager.Instance != null && NetworkManager.Instance.Room != null
            && NetworkManager.Instance.Room.State != null
            && NetworkManager.Instance.Room.State.players != null)
        {
            NetworkManager.Instance.Room.State.players.ForEach((_, ps) =>
            {
                knownPlayers++;
                if (ps.isAlive && !ps.hasFinished) alivePlayers++;
            });
        }
        if (knownPlayers >= 2 && alivePlayers == 0)
        {
            if (showDebug) Debug.Log($"GameManager: client-side all-done check passed (knownPlayers={knownPlayers}, alivePlayers={alivePlayers}) → ShowFinalGameOver");
            ShowFinalGameOver();
        }
        return;
#else
        if (playersFinished >= totalPlayers)
        {
            if (showDebug)
            {
                Debug.Log("GameManager: Tous les joueurs ont terminé! Affichage du GameOverPanel...");
            }

            gameInProgress = false;
            ShowFinalGameOver();
        }
#endif
    }

    /// <summary>
    /// Affiche le panel de fin de partie avec les résultats. Idempotent : peut être
    /// appelée plusieurs fois (mort locale, puis status="finished" serveur) — le panel
    /// reste actif et les scores sont rafraîchis à chaque appel.
    /// </summary>
    private void ShowFinalGameOver()
    {
        if (showDebug)
        {
            int localP = GameSessionManager.Instance != null ? GameSessionManager.Instance.LocalPlayerNumber : -1;
            Debug.Log($"GameManager.ShowFinalGameOver: localP={localP}, gameOverPanel={(gameOverPanel != null ? gameOverPanel.name : "NULL")}, panelActive={(gameOverPanel != null ? gameOverPanel.activeSelf : false)}");
        }

        // Cache les overlays individuels
        if (player1EliminatedOverlay != null)
        {
            player1EliminatedOverlay.SetActive(false);
        }
        if (player2EliminatedOverlay != null)
        {
            player2EliminatedOverlay.SetActive(false);
        }

        // IMPORTANT: Rafraîchit les pseudos depuis PlayerNameManager ou GameSessionManager
        RefreshPlayerNames();

        // Affiche le GameOverPanel
        if (gameOverPanel == null)
        {
            Debug.LogError("GameManager.ShowFinalGameOver: gameOverPanel Inspector reference is NULL — panel can't be shown");
        }
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
#if WEB_BUILD
            // Lift the panel to the root canvas and stretch it fullscreen so the buttons
            // are reachable regardless of the arcade panel's original split-screen layout.
            var rt = gameOverPanel.GetComponent<RectTransform>();
            if (rt != null)
            {
                var canvas = rt.GetComponentInParent<Canvas>();
                var root = canvas != null ? canvas.rootCanvas : null;
                if (root != null && rt.parent != root.transform)
                {
                    rt.SetParent(root.transform, worldPositionStays: false);
                }
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.localScale = Vector3.one;
                rt.localRotation = Quaternion.identity;
                // Force a Canvas component on the panel so we can guarantee it renders
                // above any leftover HUD elements (sortingOrder=1000 > WebWaitOverlay's 9999
                // doesn't matter since that overlay is destroyed before end of match).
                var panelCanvas = gameOverPanel.GetComponent<Canvas>();
                if (panelCanvas == null)
                {
                    panelCanvas = gameOverPanel.AddComponent<Canvas>();
                    panelCanvas.overrideSorting = true;
                    gameOverPanel.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                }
                else
                {
                    panelCanvas.overrideSorting = true;
                }
                panelCanvas.sortingOrder = 1000;
                gameOverPanel.transform.SetAsLastSibling();
            }
#endif
        }

        // Titre
        if (gameOverTitleText != null)
        {
            gameOverTitleText.text = "PARTIE TERMINÉE";
        }

        // Trouve le gagnant (meilleur score)
        int winnerID = -1;
        int highestScore = -1;
        foreach (var kvp in playerScores)
        {
            if (kvp.Value > highestScore)
            {
                highestScore = kvp.Value;
                winnerID = kvp.Key;
            }
        }

        // Affiche le gagnant
        if (winnerText != null && winnerID != -1)
        {
            string winnerName = playerNames.ContainsKey(winnerID) ? playerNames[winnerID] : $"Joueur {winnerID}";
            winnerText.text = $"🏆 {winnerName} gagne!";
            if (showDebug)
            {
                Debug.Log($"GameManager: Gagnant affiché: '{winnerName}' (ID: {winnerID})");
            }
        }

        // Affiche tous les scores
        if (scoresText != null)
        {
            string scoresDisplay = "";
            foreach (var kvp in playerScores)
            {
                string name = playerNames.ContainsKey(kvp.Key) ? playerNames[kvp.Key] : $"Joueur {kvp.Key}";
                scoresDisplay += $"{name}: {kvp.Value} pts\n";
            }
            scoresText.text = scoresDisplay;

            if (showDebug)
            {
                Debug.Log($"GameManager: Scores affichés:\n{scoresDisplay}");
            }
        }

        // Button selection is handled by MenuPanelController on the GameOverPanel itself.

        // Désactive les contrôles de tous les joueurs et fige leurs animations.
        // En WEB_BUILD, on skip le remote clone (NetworkPlayer) — sinon son anim s'arrête
        // alors qu'il est peut-être encore vivant côté serveur.
        PlayerInput[] allInputs = FindObjectsOfType<PlayerInput>();
        foreach (var input in allInputs)
        {
#if WEB_BUILD
            if (input.GetComponent<NetworkPlayer>() != null) continue;
#endif
            input.enabled = false;

            Animator anim = input.GetComponent<Animator>();
            if (anim == null) anim = input.GetComponentInChildren<Animator>();
            if (anim != null) anim.speed = 0f;

            Rigidbody rb = input.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

#if WEB_BUILD
        // Persist scores as soon as the panel shows — don't wait for the user to click
        // Quitter. ScoreManager dedupes via its hasEndedGame flag so this is idempotent
        // with the explicit QuitToMenu path.
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.SaveScoresNow(null);
        }
#endif

#if !WEB_BUILD
        // En arcade on fige la physique. En WEB_BUILD on la laisse tourner pour que
        // l'animation du remote (qui passe par NetworkPlayer.Update + Animator) reste fluide
        // tant que l'autre joueur est encore en jeu.
        Physics.autoSimulation = false;
#endif
        // Time.timeScale = 0f; // DÉSACTIVÉ pour permettre la navigation UI
    }

    /// <summary>
    /// Relance la partie
    /// </summary>
    public void RestartGame()
    {
        StartCoroutine(RestartGameCoroutine());
    }

    private IEnumerator RestartGameCoroutine()
    {
        Time.timeScale = 1f;
        Physics.autoSimulation = true; // Réactive la physique
        gameInProgress = false;

        // Réinitialise les scores localement
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetScores();
        }

        if (showDebug)
        {
            Debug.Log("GameManager: Redémarrage local (session API maintenue ouverte)");
        }

        // On ne rappelle PAS GameSessionManager.StartGame ici car cela créerait une nouvelle partie
        // ou échouerait si la session est déjà "started". On garde la session ouverte jusqu'au Quit.

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        yield return null;
    }

    /// <summary>
    /// Retourne au menu principal
    /// </summary>
    public void QuitToMenu()
    {
        Time.timeScale = 1f;
        Physics.autoSimulation = true; // Réactive la physique
        gameInProgress = false;

        // Fire-and-forget pour les scores : ShowFinalGameOver a déjà lancé le POST,
        // donc inutile de bloquer le quit en attendant. SaveScoresNow est idempotent
        // (hasEndedGame=true → no-op), il n'y a aucun risque de double envoi.
        if (ScoreManager.Instance != null)
        {
            if (showDebug) Debug.Log("GameManager: Sauvegarde des scores (fire-and-forget) avant de quitter...");
            ScoreManager.Instance.SaveScoresNow(null);
        }

        // Réinitialise la session
        if (GameSessionManager.Instance != null)
        {
            GameSessionManager.Instance.ResetSession();
        }

#if WEB_BUILD
        // Tell the Next.js iframe parent to navigate to /classement/[sessionId].
        // No MainMenu fallback in the web flow — the page itself is the menu.
        WebBridge.NotifyQuit(WebBootstrap.SessionId);
#else
        SceneManager.LoadScene("MainMenu");
#endif
    }
}
