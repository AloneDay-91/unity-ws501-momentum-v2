using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;
using TMPro;

public class GameManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    public static GameManager Instance { get; private set; }

    [Header("UI")]
    public GameObject countdownOverlay;
    public GameObject gameOverPanel;

    [Header("Game Settings")]
    public int countdownDuration = 3;

    [Header("API Integration")]
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

        // Initialize API components
        authManager = FindObjectOfType<AuthManager>();
        apiClient = FindObjectOfType<GameAPIClient>();

        // Ensure the game over panel is hidden at the start
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        StartCoroutine(StartCountdownCoroutine());
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
        // Record game start time
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
    /// Call this method when a player wins the game
    /// </summary>
    /// <param name="playerName">Name of the winning player</param>
    /// <param name="playerScore">Final score</param>
    public void OnPlayerWin(string playerName, int playerScore)
    {
        if (!gameInProgress) return;

        gameInProgress = false;
        float completionTime = Time.time - gameStartTime;

        Debug.Log($"Player {playerName} won with score {playerScore} in {completionTime:F2} seconds!");

        // Send score to API
        if (apiClient != null && authManager != null && authManager.HasValidToken())
        {
            StartCoroutine(apiClient.SaveScore(playerName, playerScore, completionTime));
        }
        else
        {
            Debug.LogWarning("API Client not ready or no token available. Score not sent.");
        }

        // Show game over panel
        ShowGameOver(playerName, playerScore, completionTime);
    }

    /// <summary>
    /// Call this method when the game ends (time's up, all players finished, etc.)
    /// </summary>
    /// <param name="playerName">Name of the player</param>
    /// <param name="finalScore">Final score</param>
    public void OnGameFinished(string playerName, int finalScore)
    {
        if (!gameInProgress) return;

        gameInProgress = false;
        float completionTime = Time.time - gameStartTime;

        Debug.Log($"Game finished for {playerName}. Score: {finalScore}, Time: {completionTime:F2}s");

        // Send score to API
        if (apiClient != null && authManager != null && authManager.HasValidToken())
        {
            StartCoroutine(apiClient.SaveScore(playerName, finalScore, completionTime));
        }

        ShowGameOver(playerName, finalScore, completionTime);
    }

    private void ShowGameOver(string playerName, int score, float time)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            // Update game over panel text (if you have TextMeshPro components)
            TextMeshProUGUI[] texts = gameOverPanel.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var text in texts)
            {
                if (text.name == "WinnerText")
                {
                    text.text = $"{playerName} wins!";
                }
                else if (text.name == "ScoreText")
                {
                    text.text = $"Score: {score}";
                }
                else if (text.name == "TimeText")
                {
                    text.text = $"Time: {time:F2}s";
                }
            }
        }

        Time.timeScale = 0f; // Pause the game
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        gameInProgress = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
