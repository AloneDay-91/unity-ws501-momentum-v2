using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Anatidae;

/// <summary>
/// Gère le scoring des joueurs et la sauvegarde des scores via l'API Next.js
/// Utilise /api/game/end pour terminer la partie et sauvegarder tous les scores
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

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
    private bool hasEndedGame = false;

    [System.Serializable]
    public class PlayerScore
    {
        public int playerID;
        public float distanceTraveled;
        public float survivalTime;
        public int collectiblesCollected;
        public bool hasFinished;      // A terminé le parcours (victoire)
        public bool isEliminated;     // A été éliminé
        public bool isGameOver;       // La partie est finie pour ce joueur (fini OU éliminé)
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
                isEliminated = false,
                isGameOver = false,
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
    /// Marque un joueur comme ayant terminé le parcours (victoire)
    /// </summary>
    public void OnPlayerFinished(int playerID)
    {
        if (playerScores.ContainsKey(playerID))
        {
            PlayerScore score = playerScores[playerID];
            score.hasFinished = true;
            score.isGameOver = true;

            // Calcule le score final
            CalculateScore(playerID);

            if (showDebug)
            {
                Debug.Log($"ScoreManager: Joueur {playerID} a terminé le parcours! Score: {score.totalScore}");
            }

            // Vérifie si tous les joueurs ont terminé
            CheckAndEndGame();
        }
    }

    /// <summary>
    /// Appelé quand un joueur est éliminé
    /// </summary>
    public void OnPlayerEliminated(int playerID)
    {
        if (playerScores.ContainsKey(playerID))
        {
            PlayerScore score = playerScores[playerID];
            score.isEliminated = true;
            score.isGameOver = true;

            // Calcule le score final
            CalculateScore(playerID);

            if (showDebug)
            {
                Debug.Log($"ScoreManager: Joueur {playerID} a été éliminé! Score: {score.totalScore}");
            }

            // Vérifie si tous les joueurs ont terminé
            CheckAndEndGame();
        }
    }

    /// <summary>
    /// Vérifie si tous les joueurs ont terminé et envoie les scores à l'API
    /// </summary>
    private void CheckAndEndGame()
    {
        // Vérifie si la partie a déjà été terminée
        if (hasEndedGame)
        {
            return;
        }

        // Vérifie si tous les joueurs ont terminé
        bool allPlayersFinished = true;
        foreach (var score in playerScores.Values)
        {
            if (!score.isGameOver)
            {
                allPlayersFinished = false;
                break;
            }
        }

        if (allPlayersFinished)
        {
            if (showDebug)
            {
                Debug.Log("ScoreManager: Tous les joueurs ont terminé! Envoi des scores à l'API...");
            }

            hasEndedGame = true;
            StartCoroutine(SendScoresToAPI());
            StartCoroutine(SendScoresToArcade());
        }
    }

    /// <summary>
    /// Envoie les scores à la borne d'arcade locale
    /// </summary>
    private IEnumerator SendScoresToArcade()
    {
        if (Anatidae.AnatidaeArcadeClient.Instance == null)
        {
            Debug.LogWarning("ScoreManager: AnatidaeArcadeClient manquant, scores non envoyés à la borne.");
            yield break;
        }

        if (GameSessionManager.Instance == null) yield break;

        foreach (var kvp in playerScores)
        {
            int playerId = kvp.Key;
            int score = kvp.Value.totalScore;
            string name = (playerId == 1) ? GameSessionManager.Instance.player1Pseudo : GameSessionManager.Instance.player2Pseudo;

            if (!string.IsNullOrEmpty(name))
            {
                if (showDebug) Debug.Log($"ScoreManager: Envoi score Arcade pour {name} ({score})...");
                
                // On attend la fin de l'envoi pour ce joueur avant de passer au suivant
                yield return Anatidae.AnatidaeArcadeClient.Instance.PostHighscore(name, score, (success) => {
                    if (showDebug) Debug.Log($"ScoreManager: Score Arcade {name} envoyé: {success}");
                });
            }
        }
    }

    /// <summary>
    /// Classes pour sérialiser les données en JSON
    /// </summary>
    [System.Serializable]
    private class EndGameRequest
    {
        public string sessionId;
        public PlayerScoreData[] scores;
    }

    [System.Serializable]
    private class PlayerScoreData
    {
        public int playerNumber;
        public int totalScore;
        public float distanceTraveled;
        public float survivalTime;
        public int collectiblesCollected;
        public bool hasFinished;
    }

    /// <summary>
    /// Envoie tous les scores à l'API /api/game/end
    /// </summary>
    private IEnumerator SendScoresToAPI()
    {
        // Récupère le sessionId depuis GameSessionManager
        string sessionId = null;
        string apiBaseUrl = "http://localhost:3000";

        if (GameSessionManager.Instance != null)
        {
            sessionId = GameSessionManager.Instance.sessionId;
            apiBaseUrl = GameSessionManager.Instance.apiBaseUrl.Trim();
        }

        if (string.IsNullOrEmpty(sessionId))
        {
            Debug.LogWarning("ScoreManager: Pas de sessionId, impossible de sauvegarder les scores via /api/game/end");
            yield break;
        }

        // Prépare les données des scores
        List<PlayerScoreData> scoresList = new List<PlayerScoreData>();

        foreach (var kvp in playerScores)
        {
            PlayerScore score = kvp.Value;
            scoresList.Add(new PlayerScoreData
            {
                playerNumber = score.playerID,
                totalScore = score.totalScore,
                distanceTraveled = score.distanceTraveled,
                survivalTime = score.survivalTime,
                collectiblesCollected = score.collectiblesCollected,
                hasFinished = score.hasFinished
            });
        }

        // Crée la requête
        EndGameRequest request = new EndGameRequest
        {
            sessionId = sessionId,
            scores = scoresList.ToArray()
        };

        string jsonData = JsonUtility.ToJson(request);
        string url = $"{apiBaseUrl}/api/game/end";

        if (showDebug)
        {
            Debug.Log($"ScoreManager: Envoi des scores à {url}");
            Debug.Log($"ScoreManager: Données: {jsonData}");
        }

        // Utilise AnatidaeProxyWebRequest pour contourner CORS en WebGL
        using (UnityWebRequest webRequest = AnatidaeProxyWebRequest.Post(url, jsonData, "application/json"))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                if (showDebug)
                {
                    Debug.Log($"ScoreManager: Scores sauvegardés avec succès!");
                    Debug.Log($"ScoreManager: Réponse: {webRequest.downloadHandler.text}");
                }
            }
            else
            {
                Debug.LogError($"ScoreManager: Erreur lors de la sauvegarde des scores - {webRequest.error}");
                Debug.LogError($"ScoreManager: Réponse: {webRequest.downloadHandler.text}");
            }
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

    /// <summary>
    /// Réinitialise les scores (à appeler au début d'une nouvelle partie)
    /// </summary>
    public void ResetScores()
    {
        playerScores.Clear();
        hasEndedGame = false;

        if (showDebug)
        {
            Debug.Log("ScoreManager: Scores réinitialisés");
        }
    }
}
