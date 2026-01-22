using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Suit la progression des joueurs sur le parcours
/// Place ce script sur un GameObject vide (ex: "ProgressionTracker")
/// </summary>
public class ProgressionTracker : MonoBehaviour
{
    public static ProgressionTracker Instance { get; private set; }

    [Header("Course Settings")]
    [Tooltip("Position de départ du parcours (X)")]
    public float startPositionX = 0f;

    [Tooltip("Position de fin du parcours (X)")]
    public float endPositionX = 500f;

    [Tooltip("Trouve automatiquement les joueurs")]
    public bool autoFindPlayers = true;

    [Tooltip("Liste manuelle des joueurs à suivre")]
    public List<Transform> players = new List<Transform>();

    [Header("Laser Wall")]
    [Tooltip("Référence au mur de laser (optionnel)")]
    public LaserWall laserWall;

    [Tooltip("Trouve automatiquement le mur de laser")]
    public bool autoFindLaserWall = true;

    [Header("Debug")]
    [Tooltip("Afficher les infos de debug")]
    public bool showDebug = true;

    [Tooltip("Afficher les gizmos")]
    public bool showGizmos = true;

    // Données de progression
    private Dictionary<Transform, float> playerProgression = new Dictionary<Transform, float>();
    private Transform leadingPlayer;
    private float laserWallProgress = 0f;

    void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Trouve les joueurs
        if (autoFindPlayers)
        {
            FindPlayers();
        }

        // Trouve le mur de laser
        if (autoFindLaserWall && laserWall == null)
        {
            laserWall = FindObjectOfType<LaserWall>();
        }

        if (showDebug)
        {
            Debug.Log($"ProgressionTracker: Suivi de {players.Count} joueurs");
            Debug.Log($"Parcours: {startPositionX} → {endPositionX} ({GetCourseLength()} unités)");
            if (players.Count > 0)
            {
                foreach (var player in players)
                {
                    Debug.Log($"  - Joueur: {player.name} à position X={player.position.x}");
                }
            }
            if (laserWall != null)
            {
                Debug.Log($"LaserWall trouvé à position X={laserWall.transform.position.x}");
            }
            else
            {
                Debug.LogWarning("LaserWall non trouvé!");
            }
        }
    }

    void Update()
    {
        UpdatePlayerProgressions();
        UpdateLeadingPlayer();
        UpdateLaserWallProgress();

        // Debug périodique
        if (showDebug && Time.frameCount % 120 == 0 && players.Count > 0)
        {
            Debug.Log("=== ProgressionTracker Update ===");
            foreach (Transform player in players)
            {
                if (player == null) continue;
                float progress = GetPlayerProgress(player);
                Debug.Log($"  {player.name}: X={player.position.x:F1}, Progress={progress:F2} ({progress * 100:F0}%)");
            }
            if (laserWall != null)
            {
                Debug.Log($"  LaserWall: X={laserWall.transform.position.x:F1}, Progress={laserWallProgress:F2}");
            }
        }
    }

    private void FindPlayers()
    {
        players.Clear();
        PlayerInput[] playerInputs = FindObjectsOfType<PlayerInput>();

        foreach (PlayerInput playerInput in playerInputs)
        {
            players.Add(playerInput.transform);
        }
    }

    private void UpdatePlayerProgressions()
    {
        foreach (Transform player in players)
        {
            if (player == null) continue;

            float progress = CalculateProgress(player.position.x);
            playerProgression[player] = progress;
        }
    }

    private void UpdateLeadingPlayer()
    {
        if (players.Count == 0) return;

        leadingPlayer = players.OrderByDescending(p => p != null ? p.position.x : float.MinValue).FirstOrDefault();
    }

    private void UpdateLaserWallProgress()
    {
        if (laserWall != null)
        {
            laserWallProgress = CalculateProgress(laserWall.transform.position.x);
        }
    }

    /// <summary>
    /// Calcule la progression en pourcentage (0 à 1) selon la position X
    /// </summary>
    public float CalculateProgress(float positionX)
    {
        float courseLength = endPositionX - startPositionX;
        if (courseLength <= 0) return 0f;

        float relativePosition = positionX - startPositionX;
        float progress = Mathf.Clamp01(relativePosition / courseLength);
        return progress;
    }

    /// <summary>
    /// Obtient la progression d'un joueur spécifique (0 à 1)
    /// </summary>
    public float GetPlayerProgress(Transform player)
    {
        if (player == null) return 0f;

        if (playerProgression.ContainsKey(player))
        {
            return playerProgression[player];
        }

        return CalculateProgress(player.position.x);
    }

    /// <summary>
    /// Obtient la progression du mur de laser (0 à 1)
    /// </summary>
    public float GetLaserWallProgress()
    {
        return laserWallProgress;
    }

    /// <summary>
    /// Obtient le joueur en tête
    /// </summary>
    public Transform GetLeadingPlayer()
    {
        return leadingPlayer;
    }

    /// <summary>
    /// Obtient la distance entre un joueur et le mur de laser
    /// </summary>
    public float GetDistanceFromLaserWall(Transform player)
    {
        if (player == null || laserWall == null) return float.MaxValue;

        return player.position.x - laserWall.transform.position.x;
    }

    /// <summary>
    /// Obtient la distance parcourue par le joueur depuis le départ
    /// </summary>
    public float GetDistanceTraveled(Transform player)
    {
        if (player == null) return 0f;
        return Mathf.Max(0f, player.position.x - startPositionX);
    }

    /// <summary>
    /// Vérifie si un joueur est en danger (proche du mur)
    /// </summary>
    public bool IsPlayerInDanger(Transform player, float dangerDistance = 10f)
    {
        float distance = GetDistanceFromLaserWall(player);
        return distance < dangerDistance;
    }

    /// <summary>
    /// Obtient la longueur totale du parcours
    /// </summary>
    public float GetCourseLength()
    {
        return endPositionX - startPositionX;
    }

    /// <summary>
    /// Obtient le classement des joueurs (1 = premier, 2 = deuxième, etc.)
    /// </summary>
    public int GetPlayerRank(Transform player)
    {
        if (player == null) return players.Count;

        var sortedPlayers = players
            .Where(p => p != null)
            .OrderByDescending(p => p.position.x)
            .ToList();

        return sortedPlayers.IndexOf(player) + 1;
    }

    /// <summary>
    /// Obtient tous les joueurs triés par progression
    /// </summary>
    public List<Transform> GetPlayersSortedByProgress()
    {
        return players
            .Where(p => p != null)
            .OrderByDescending(p => p.position.x)
            .ToList();
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // Dessine la ligne de départ
        Gizmos.color = Color.green;
        Gizmos.DrawLine(
            new Vector3(startPositionX, -10, 0),
            new Vector3(startPositionX, 10, 0)
        );

        // Dessine la ligne d'arrivée
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(
            new Vector3(endPositionX, -10, 0),
            new Vector3(endPositionX, 10, 0)
        );

        // Dessine la zone du parcours
        Gizmos.color = new Color(0, 1, 1, 0.1f);
        Vector3 center = new Vector3((startPositionX + endPositionX) / 2f, 0, 0);
        Vector3 size = new Vector3(endPositionX - startPositionX, 20, 1);
        Gizmos.DrawCube(center, size);

        // Dessine les positions des joueurs
        if (Application.isPlaying && players.Count > 0)
        {
            foreach (Transform player in players)
            {
                if (player == null) continue;

                Gizmos.color = player == leadingPlayer ? Color.yellow : Color.white;
                Gizmos.DrawWireSphere(player.position, 1f);

                // Dessine une ligne verticale pour montrer la progression
                float progress = GetPlayerProgress(player);
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(
                    player.position,
                    new Vector3(player.position.x, player.position.y + 5f, player.position.z)
                );
            }
        }
    }
}
