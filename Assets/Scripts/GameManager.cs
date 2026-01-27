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
    private bool gameInProgress = false;

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

        StartCoroutine(StartCountdownCoroutine());
    }

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

        // Affiche l'overlay individuel pour ce joueur
        ShowPlayerEliminatedOverlay(playerID, playerName, score);

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
        if (playersFinished >= totalPlayers)
        {
            if (showDebug)
            {
                Debug.Log("GameManager: Tous les joueurs ont terminé! Affichage du GameOverPanel...");
            }

            gameInProgress = false;
            ShowFinalGameOver();
        }
    }

    /// <summary>
    /// Affiche le panel de fin de partie avec les résultats
    /// </summary>
    private void ShowFinalGameOver()
    {
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
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
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

        // La sélection du premier bouton est maintenant gérée par MenuPanelController sur le GameOverPanel
        /*
        if (gameOverFirstButton != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(gameOverFirstButton);
            if (showDebug) Debug.Log("GameManager: Bouton GameOver sélectionné");
        }
        */

        // Désactive les contrôles de tous les joueurs et fige leurs animations
        PlayerInput[] allInputs = FindObjectsOfType<PlayerInput>();
        foreach (var input in allInputs)
        {
            input.enabled = false;
            
            // Fige l'animation
            Animator anim = input.GetComponent<Animator>();
            if (anim == null) anim = input.GetComponentInChildren<Animator>();
            if (anim != null) anim.speed = 0f;
            
            // Coupe la vélocité si Rigidbody
            Rigidbody rb = input.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true; // Empêche tout mouvement physique résiduel
            }
        }

        // Libère la souris (au cas où)
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Pause la physique au lieu du temps (pour que l'UI continue de fonctionner)
        Physics.autoSimulation = false;
        // Time.timeScale = 0f; // DÉSACTIVÉ pour permettre la navigation UI
    }

    // --- MÉTHODES LEGACY (pour compatibilité) ---

    /// <summary>
    /// [LEGACY] Call this method when a player wins the game
    /// </summary>
    public void OnPlayerWin(string playerName, int playerScore)
    {
        // Trouve le playerID à partir du nom
        int playerID = 1;
        foreach (var kvp in playerNames)
        {
            if (kvp.Value == playerName)
            {
                playerID = kvp.Key;
                break;
            }
        }

        OnPlayerFinished(playerID, playerScore);
    }

    /// <summary>
    /// [LEGACY] Call this method when the game ends
    /// </summary>
    public void OnGameFinished(string playerName, int finalScore)
    {
        // Trouve le playerID à partir du nom
        int playerID = 1;
        foreach (var kvp in playerNames)
        {
            if (kvp.Value == playerName)
            {
                playerID = kvp.Key;
                break;
            }
        }

        OnPlayerEliminated(playerID, finalScore);
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
        StartCoroutine(QuitToMenuCoroutine());
    }

    private IEnumerator QuitToMenuCoroutine()
    {
        Time.timeScale = 1f;
        Physics.autoSimulation = true; // Réactive la physique
        gameInProgress = false;

        // Sauvegarde les scores avant de quitter (même si les joueurs n'ont pas tous terminé)
        bool scoresSaved = false;
        bool scoresComplete = false;

        if (ScoreManager.Instance != null)
        {
            if (showDebug)
            {
                Debug.Log("GameManager: Sauvegarde des scores avant de quitter...");
            }
            ScoreManager.Instance.SaveScoresNow((success) =>
            {
                scoresSaved = success;
                scoresComplete = true;
                if (showDebug)
                {
                    Debug.Log($"GameManager: Scores sauvegardés: {success}");
                }
            });

            // Attend que la sauvegarde soit terminée (timeout de 5 secondes)
            float timeout = 5f;
            float elapsed = 0f;
            while (!scoresComplete && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }

            if (!scoresComplete && showDebug)
            {
                Debug.LogWarning("GameManager: Timeout lors de la sauvegarde des scores");
            }
        }

        // Réinitialise la session
        if (GameSessionManager.Instance != null)
        {
            GameSessionManager.Instance.ResetSession();
        }

        SceneManager.LoadScene("MainMenu");
    }
}
