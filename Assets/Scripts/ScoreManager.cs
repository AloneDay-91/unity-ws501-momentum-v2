using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Gère le scoring des joueurs et la sauvegarde des scores via l'API Next.js
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("API Configuration")]
    [Tooltip("URL de base de l'API Next.js")]
    public string apiBaseUrl = "http://localhost:3000";

    [Header("Score Settings")]
    [Tooltip("Nom de la carte actuelle (pour différencier les scores par carte)")]
    public string currentMapName = "Map1";

    [Tooltip("Points par unité de distance parcourue")]
    public float pointsPerDistance = 10f;

    [Tooltip("Points par seconde de survie")]
    public float pointsPerSecond = 5f;

    [Tooltip("Bonus pour avoir terminé le parcours")]
    public int finishBonus = 1000;

    [Header("Debug")]
    public bool showDebug = true;

    // Scores des joueurs
    private Dictionary<int, PlayerScore> playerScores = new Dictionary<int, PlayerScore>();

    [System.Serializable]
    public class PlayerScore
    {
        public int playerID;
        public float distanceTraveled;
        public float survivalTime;
        public int collectiblesCollected;
        public bool hasFinished;
        public bool hasBeenSaved;
        public int totalScore;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (showDebug)
        {
            Debug.Log("ScoreManager: Démarré et prêt à sauvegarder les scores");
        }
    }

    /// <summary>
    /// Enregistre un joueur pour le tracking
    /// </summary>
    public void RegisterPlayer(int playerID)
    {
        if (!playerScores.ContainsKey(playerID))
        {
            playerScores[playerID] = new PlayerScore
            {
                playerID = playerID,
                distanceTraveled = 0f,
                survivalTime = 0f,
                collectiblesCollected = 0,
                hasFinished = false,
                hasBeenSaved = false,
                totalScore = 0
            };

            if (showDebug)
            {
                Debug.Log($"ScoreManager: Joueur {playerID} enregistré");
            }
        }
    }

    /// <summary>
    /// Met à jour la distance parcourue d'un joueur
    /// </summary>
    public void UpdatePlayerDistance(int playerID, float distance)
    {
        if (playerScores.ContainsKey(playerID))
        {
            playerScores[playerID].distanceTraveled = distance;
        }
    }

    /// <summary>
    /// Met à jour le temps de survie d'un joueur
    /// </summary>
    public void UpdatePlayerTime(int playerID, float time)
    {
        if (playerScores.ContainsKey(playerID))
        {
            playerScores[playerID].survivalTime = time;
        }
    }

    /// <summary>
    /// Ajoute un collectible au score du joueur
    /// </summary>
    public void AddCollectible(int playerID)
    {
        if (playerScores.ContainsKey(playerID))
        {
            playerScores[playerID].collectiblesCollected++;
        }
    }

    /// <summary>
    /// Calcule le score total d'un joueur
    /// </summary>
    public int CalculateScore(int playerID)
    {
        if (!playerScores.ContainsKey(playerID))
        {
            return 0;
        }

        PlayerScore score = playerScores[playerID];

        int distanceScore = Mathf.RoundToInt(score.distanceTraveled * pointsPerDistance);
        int timeScore = Mathf.RoundToInt(score.survivalTime * pointsPerSecond);
        int collectibleScore = score.collectiblesCollected * 50; // 50 points par collectible
        int bonus = score.hasFinished ? finishBonus : 0;

        score.totalScore = distanceScore + timeScore + collectibleScore + bonus;

        if (showDebug)
        {
            Debug.Log($"Score Joueur {playerID}: Distance={distanceScore}, Temps={timeScore}, Collectibles={collectibleScore}, Bonus={bonus}, TOTAL={score.totalScore}");
        }

        return score.totalScore;
    }

    /// <summary>
    /// Marque un joueur comme ayant terminé le parcours
    /// </summary>
    public void OnPlayerFinished(int playerID)
    {
        if (playerScores.ContainsKey(playerID))
        {
            playerScores[playerID].hasFinished = true;

            if (showDebug)
            {
                Debug.Log($"ScoreManager: Joueur {playerID} a terminé le parcours!");
            }

            // Sauvegarde le score
            StartCoroutine(SavePlayerScore(playerID));
        }
    }

    /// <summary>
    /// Appelé quand un joueur est éliminé
    /// </summary>
    public void OnPlayerEliminated(int playerID)
    {
        if (playerScores.ContainsKey(playerID))
        {
            if (showDebug)
            {
                Debug.Log($"ScoreManager: Joueur {playerID} a été éliminé");
            }

            // Sauvegarde le score même si éliminé
            StartCoroutine(SavePlayerScore(playerID));
        }
    }

    /// <summary>
    /// Classe pour sérialiser les données de score en JSON
    /// </summary>
    [System.Serializable]
    private class ScoreData
    {
        public string playerName;
        public int playerID;
        public string mapName;
        public int totalScore;
        public float distanceTraveled;
        public float survivalTime;
        public int collectiblesCollected;
        public bool hasFinished;
    }

    /// <summary>
    /// Sauvegarde le score d'un joueur via l'API Next.js
    /// </summary>
    private IEnumerator SavePlayerScore(int playerID)
    {
        if (!playerScores.ContainsKey(playerID))
        {
            yield break;
        }

        PlayerScore score = playerScores[playerID];

        // Évite de sauvegarder plusieurs fois
        if (score.hasBeenSaved)
        {
            yield break;
        }

        // Calcule le score final
        int finalScore = CalculateScore(playerID);

        // Récupère le pseudo du joueur depuis PlayerNameManager
        string playerName = "Player " + playerID;
        if (PlayerNameManager.Instance != null)
        {
            playerName = PlayerNameManager.Instance.GetPlayerName(playerID);
        }

        if (showDebug)
        {
            Debug.Log($"ScoreManager: Sauvegarde du score pour {playerName} = {finalScore}");
        }

        // Prépare les données à envoyer
        ScoreData scoreData = new ScoreData
        {
            playerName = playerName,
            playerID = playerID,
            mapName = currentMapName,
            totalScore = finalScore,
            distanceTraveled = score.distanceTraveled,
            survivalTime = score.survivalTime,
            collectiblesCollected = score.collectiblesCollected,
            hasFinished = score.hasFinished
        };

        // Convertit en JSON
        string jsonData = JsonUtility.ToJson(scoreData);

        // Envoie à l'API
        string url = $"{apiBaseUrl}/api/scores";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                if (showDebug)
                {
                    Debug.Log($"ScoreManager: Score sauvegardé avec succès pour {playerName}!");
                }
                score.hasBeenSaved = true;
            }
            else
            {
                Debug.LogError($"ScoreManager: Erreur lors de la sauvegarde du score - {request.error}");
            }
        }

        // Vérifie si tous les joueurs ont terminé
        CheckAllPlayersFinished();
    }

    /// <summary>
    /// Vérifie si tous les joueurs ont terminé (fini ou éliminé)
    /// </summary>
    private void CheckAllPlayersFinished()
    {
        bool allFinished = true;
        foreach (var score in playerScores.Values)
        {
            if (!score.hasBeenSaved)
            {
                allFinished = false;
                break;
            }
        }

        if (allFinished && showDebug)
        {
            Debug.Log("ScoreManager: Tous les joueurs ont terminé!");
            // Ici tu peux afficher un écran de fin de partie global
        }
    }

    /// <summary>
    /// Récupère le score d'un joueur
    /// </summary>
    public int GetPlayerScore(int playerID)
    {
        if (playerScores.ContainsKey(playerID))
        {
            return playerScores[playerID].totalScore;
        }
        return 0;
    }

    /// <summary>
    /// Récupère les données de score d'un joueur
    /// </summary>
    public PlayerScore GetPlayerScoreData(int playerID)
    {
        if (playerScores.ContainsKey(playerID))
        {
            return playerScores[playerID];
        }
        return null;
    }
}
