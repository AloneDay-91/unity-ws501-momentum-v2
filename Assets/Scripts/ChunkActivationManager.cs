using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gère l'activation et la désactivation des chunks selon la position des joueurs
/// Place ce script sur un GameObject vide dans ta scène (ex: "ChunkManager")
/// </summary>
public class ChunkActivationManager : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Liste des joueurs à suivre (laisse vide pour auto-détection)")]
    public List<Transform> players = new List<Transform>();

    [Tooltip("Trouve automatiquement tous les chunks avec le composant Chunk")]
    public bool autoFindChunks = true;

    [Tooltip("Liste manuelle des chunks (si autoFindChunks est false)")]
    public List<Chunk> manualChunkList = new List<Chunk>();

    [Header("Distance Settings")]
    [Tooltip("Distance en avant du joueur où les chunks sont activés")]
    public float activationDistanceForward = 50f;

    [Tooltip("Distance en arrière du joueur où les chunks sont activés")]
    public float activationDistanceBackward = 30f;

    [Tooltip("Nombre de chunks à garder actifs en avant (si tu veux utiliser un nombre plutôt qu'une distance)")]
    public int chunksToKeepActiveForward = 3;

    [Tooltip("Nombre de chunks à garder actifs en arrière")]
    public int chunksToKeepActiveBackward = 1;

    [Header("Mode")]
    [Tooltip("Utiliser la distance ou le nombre de chunks pour l'activation")]
    public ActivationMode mode = ActivationMode.Distance;

    [Header("Performance")]
    [Tooltip("Fréquence de mise à jour (en secondes). Plus bas = plus précis mais plus coûteux")]
    public float updateInterval = 0.5f;

    [Header("Debug")]
    [Tooltip("Afficher les infos de debug dans la console")]
    public bool showDebugLogs = false;

    [Tooltip("Afficher des gizmos pour visualiser les zones d'activation")]
    public bool showDebugGizmos = true;

    // État interne
    private List<Chunk> allChunks = new List<Chunk>();
    private float updateTimer = 0f;
    private float furthestPlayerPosition = 0f;
    private float nearestPlayerPosition = 0f;

    public enum ActivationMode
    {
        Distance,      // Active selon la distance
        ChunkCount     // Active un nombre fixe de chunks
    }

    void Start()
    {
        // Trouve les joueurs automatiquement si la liste est vide
        if (players.Count == 0)
        {
            FindPlayers();
        }

        // Trouve les chunks
        if (autoFindChunks)
        {
            FindAllChunks();
        }
        else
        {
            allChunks = new List<Chunk>(manualChunkList);
        }

        // Trie les chunks par position X
        allChunks.Sort((a, b) => a.chunkPosition.CompareTo(b.chunkPosition));

        if (showDebugLogs)
        {
            Debug.Log($"ChunkActivationManager: {allChunks.Count} chunks trouvés, {players.Count} joueurs");
        }

        // Première mise à jour immédiate
        UpdateChunkActivation();
    }

    void Update()
    {
        // Mise à jour périodique pour économiser les ressources
        updateTimer += Time.deltaTime;

        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            UpdateChunkActivation();
        }
    }

    private void FindPlayers()
    {
        PlayerInput[] playerInputs = FindObjectsOfType<PlayerInput>();
        foreach (PlayerInput playerInput in playerInputs)
        {
            players.Add(playerInput.transform);
        }

        if (players.Count == 0)
        {
            Debug.LogWarning("ChunkActivationManager: Aucun joueur trouvé ! Assure-toi que tes joueurs ont le composant PlayerInput.");
        }
    }

    private void FindAllChunks()
    {
        Chunk[] chunks = FindObjectsOfType<Chunk>(true); // true = inclut les objets désactivés
        allChunks = new List<Chunk>(chunks);

        if (allChunks.Count == 0)
        {
            Debug.LogWarning("ChunkActivationManager: Aucun chunk trouvé ! Assure-toi d'avoir des GameObjects avec le composant Chunk.");
        }
    }

    private void UpdateChunkActivation()
    {
        if (players.Count == 0 || allChunks.Count == 0) return;

        // Trouve la position du joueur le plus en avant ET le plus en arrière
        furthestPlayerPosition = float.MinValue;
        nearestPlayerPosition = float.MaxValue;

        foreach (Transform player in players)
        {
            if (player != null)
            {
                if (player.position.x > furthestPlayerPosition)
                {
                    furthestPlayerPosition = player.position.x;
                }
                if (player.position.x < nearestPlayerPosition)
                {
                    nearestPlayerPosition = player.position.x;
                }
            }
        }

        // Active/désactive les chunks selon le mode
        if (mode == ActivationMode.Distance)
        {
            UpdateByDistance();
        }
        else
        {
            UpdateByChunkCount();
        }
    }

    private void UpdateByDistance()
    {
        // Calcule les limites d'activation en englobant tous les joueurs
        float minActivationPosition = nearestPlayerPosition - activationDistanceBackward;
        float maxActivationPosition = furthestPlayerPosition + activationDistanceForward;

        foreach (Chunk chunk in allChunks)
        {
            // Utilise la distance custom si définie, sinon vérifie si le chunk est dans la zone globale
            bool shouldBeActive = false;

            if (chunk.customActivationDistance > 0)
            {
                // Si distance custom, vérifie pour chaque joueur individuellement
                foreach (Transform player in players)
                {
                    if (player != null)
                    {
                        float distanceToPlayer = Mathf.Abs(chunk.chunkPosition - player.position.x);
                        if (distanceToPlayer <= chunk.customActivationDistance)
                        {
                            shouldBeActive = true;
                            break;
                        }
                    }
                }
            }
            else
            {
                // Sinon, active si le chunk est dans la zone globale qui englobe tous les joueurs
                shouldBeActive = chunk.chunkPosition >= minActivationPosition &&
                                 chunk.chunkPosition <= maxActivationPosition;
            }

            if (shouldBeActive)
            {
                chunk.Activate();
            }
            else
            {
                chunk.Deactivate();
            }
        }
    }

    private void UpdateByChunkCount()
    {
        // Désactive tous les chunks d'abord
        foreach (Chunk chunk in allChunks)
        {
            chunk.Deactivate();
        }

        // Active les chunks autour de chaque joueur
        foreach (Transform player in players)
        {
            if (player == null) continue;

            // Trouve l'index du chunk le plus proche de ce joueur
            int closestChunkIndex = FindClosestChunkIndex(player.position.x);

            if (closestChunkIndex == -1) continue;

            // Active les chunks dans la plage autour de ce joueur
            for (int i = 0; i < allChunks.Count; i++)
            {
                int distanceFromPlayer = i - closestChunkIndex;

                bool shouldBeActive = distanceFromPlayer <= chunksToKeepActiveForward &&
                                      distanceFromPlayer >= -chunksToKeepActiveBackward;

                if (shouldBeActive)
                {
                    allChunks[i].Activate();
                }
            }
        }
    }

    private int FindClosestChunkIndex(float positionX)
    {
        int closestIndex = -1;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < allChunks.Count; i++)
        {
            float distance = Mathf.Abs(allChunks[i].chunkPosition - positionX);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    void OnDrawGizmos()
    {
        if (!showDebugGizmos || players.Count == 0) return;

        // Dessine les zones d'activation
        if (mode == ActivationMode.Distance)
        {
            // Zone globale qui englobe tous les joueurs
            float minActivationPosition = nearestPlayerPosition - activationDistanceBackward;
            float maxActivationPosition = furthestPlayerPosition + activationDistanceForward;
            float centerPosition = (minActivationPosition + maxActivationPosition) / 2f;
            float totalWidth = maxActivationPosition - minActivationPosition;

            Gizmos.color = new Color(0, 1, 0, 0.15f);
            Vector3 center = new Vector3(centerPosition, 0, 0);
            Vector3 size = new Vector3(totalWidth, 100, 100);
            Gizmos.DrawCube(center, size);

            // Dessine les marges pour chaque joueur
            foreach (Transform player in players)
            {
                if (player == null) continue;

                // Zone en avant du joueur
                Gizmos.color = new Color(0, 1, 1, 0.2f);
                Vector3 forwardCenter = new Vector3(
                    player.position.x + activationDistanceForward / 2f,
                    0,
                    0
                );
                Vector3 forwardSize = new Vector3(activationDistanceForward, 80, 80);
                Gizmos.DrawWireCube(forwardCenter, forwardSize);

                // Zone en arrière du joueur
                Gizmos.color = new Color(1, 1, 0, 0.2f);
                Vector3 backwardCenter = new Vector3(
                    player.position.x - activationDistanceBackward / 2f,
                    0,
                    0
                );
                Vector3 backwardSize = new Vector3(activationDistanceBackward, 80, 80);
                Gizmos.DrawWireCube(backwardCenter, backwardSize);

                // Position du joueur
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(player.position, 2f);
            }
        }
    }

    /// <summary>
    /// Force une mise à jour immédiate (utile si tu téléportes un joueur par exemple)
    /// </summary>
    public void ForceUpdate()
    {
        UpdateChunkActivation();
    }

    /// <summary>
    /// Ajoute un chunk manuellement à la liste
    /// </summary>
    public void AddChunk(Chunk chunk)
    {
        if (!allChunks.Contains(chunk))
        {
            allChunks.Add(chunk);
            allChunks.Sort((a, b) => a.chunkPosition.CompareTo(b.chunkPosition));
        }
    }

    /// <summary>
    /// Retire un chunk de la liste
    /// </summary>
    public void RemoveChunk(Chunk chunk)
    {
        allChunks.Remove(chunk);
    }
}
