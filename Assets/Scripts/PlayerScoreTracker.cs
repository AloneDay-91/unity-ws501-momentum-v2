using UnityEngine;

/// <summary>
/// Suit la progression du joueur et met à jour son score en temps réel
/// Attache ce script sur chaque joueur
/// </summary>
public class PlayerScoreTracker : MonoBehaviour
{
    [Header("References")]
    private PlayerInput playerInput;
    private ProgressionTracker progressionTracker;

    [Header("Tracking")]
    private float gameStartTime;
    private bool isTracking = false;
    private float startPositionX;

    void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            Debug.LogError("PlayerScoreTracker: PlayerInput non trouvé!");
            enabled = false;
            return;
        }

        progressionTracker = ProgressionTracker.Instance;
        if (progressionTracker == null)
        {
            Debug.LogWarning("PlayerScoreTracker: ProgressionTracker non trouvé!");
        }

        startPositionX = transform.position.x;

        // Enregistre ce joueur auprès du ScoreManager
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.RegisterPlayer(playerInput.playerID);
        }
    }

    /// <summary>
    /// Démarre le tracking (appelé par GameManager après le countdown)
    /// </summary>
    public void StartTracking()
    {
        isTracking = true;
        gameStartTime = Time.time;
        startPositionX = transform.position.x;

        Debug.Log($"PlayerScoreTracker: Tracking démarré pour joueur {playerInput.playerID}");
    }

    void Update()
    {
        if (!isTracking || ScoreManager.Instance == null || playerInput == null)
        {
            return;
        }

        // Met à jour le temps de survie
        float survivalTime = Time.time - gameStartTime;
        ScoreManager.Instance.UpdatePlayerTime(playerInput.playerID, survivalTime);

        // Met à jour la distance parcourue
        float distanceTraveled = transform.position.x - startPositionX;
        if (progressionTracker != null)
        {
            distanceTraveled = transform.position.x - progressionTracker.startPositionX;
        }
        ScoreManager.Instance.UpdatePlayerDistance(playerInput.playerID, distanceTraveled);
    }

    /// <summary>
    /// Arrête le tracking
    /// </summary>
    public void StopTracking()
    {
        isTracking = false;
        Debug.Log($"PlayerScoreTracker: Tracking arrêté pour joueur {playerInput.playerID}");
    }

    void OnDisable()
    {
        // Arrête le tracking si le joueur est désactivé
        StopTracking();
    }
}
