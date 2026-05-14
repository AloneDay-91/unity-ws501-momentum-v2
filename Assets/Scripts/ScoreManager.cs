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

    [Tooltip("Points par orbe de lumière collecté")]
    public int pointsPerOrb = 10;

    [Tooltip("Points par saut parfait (bon timing sur obstacle)")]
    public int pointsPerPerfectJump = 100;

    [Tooltip("Bonus pour avoir terminé le parcours")]
    public int finishBonus = 1000;

    [Tooltip("Points par unité de distance (désactivé si 0)")]
    public float pointsPerDistance = 0f;

    [Tooltip("Points par seconde de survie (désactivé si 0)")]
    public float pointsPerSecond = 0f;

    [Header("Debug")]
    public bool showDebug = true;

    // Événement déclenché quand le score d'un joueur change
    public event System.Action<int, int> OnScoreChanged;

    // Scores des joueurs
    private Dictionary<int, PlayerScore> playerScores = new Dictionary<int, PlayerScore>();
    private bool hasEndedGame = false;

    [System.Serializable]
    public class PlayerScore
    {
        public int playerID;
        public float distanceTraveled;
        public float survivalTime;
        public int orbsCollected;         // Orbes de lumière collectés
        public int perfectJumps;          // Sauts parfaits sur obstacles
        public bool hasFinished;          // A terminé le parcours (victoire)
        public bool isEliminated;         // A été éliminé
        public bool isGameOver;           // La partie est finie pour ce joueur (fini OU éliminé)
        public int totalScore;

        // Legacy
        [System.Obsolete("Utilisez orbsCollected à la place")]
        public int collectiblesCollected => orbsCollected;
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
                orbsCollected = 0,
                perfectJumps = 0,
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
    /// Ajoute un orbe de lumière au score du joueur
    /// </summary>
    public void AddOrb(int playerID)
    {
        if (playerScores.ContainsKey(playerID))
        {
            playerScores[playerID].orbsCollected++;

            // Recalcule le score pour déclencher l'événement et les effets "juice"
            CalculateScore(playerID);

            if (showDebug)
            {
                Debug.Log($"ScoreManager: Joueur {playerID} a collecté un orbe (+{pointsPerOrb} pts)");
            }
        }
    }

    /// <summary>
    /// Ajoute un saut parfait au score du joueur
    /// </summary>
    public void AddPerfectJump(int playerID)
    {
        if (playerScores.ContainsKey(playerID))
        {
            playerScores[playerID].perfectJumps++;

            // Recalcule le score pour déclencher l'événement et les effets "juice"
            CalculateScore(playerID);

            if (showDebug)
            {
                Debug.Log($"ScoreManager: Joueur {playerID} a réussi un saut parfait (+{pointsPerPerfectJump} pts)");
            }
        }
    }

    /// <summary>
    /// [OBSOLETE] Ajoute un collectible au score du joueur - Utilisez AddOrb() à la place
    /// </summary>
    [System.Obsolete("Utilisez AddOrb() à la place")]
    public void AddCollectible(int playerID)
    {
        AddOrb(playerID);
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

        // Nouveau système de scoring basé sur les actions
        int orbScore = score.orbsCollected * pointsPerOrb;                    // 10 pts par orbe
        int jumpScore = score.perfectJumps * pointsPerPerfectJump;            // 100 pts par saut parfait
        int finishScore = score.hasFinished ? finishBonus : 0;                // 1000 pts pour terminer

        // Ancien système (optionnel, désactivé par défaut)
        int distanceScore = Mathf.RoundToInt(score.distanceTraveled * pointsPerDistance);
        int timeScore = Mathf.RoundToInt(score.survivalTime * pointsPerSecond);

        int oldScore = score.totalScore;
        score.totalScore = orbScore + jumpScore + finishScore + distanceScore + timeScore;

        if (showDebug)
        {
            Debug.Log($"Score Joueur {playerID}: Orbes={orbScore}, Sauts Parfaits={jumpScore}, Terminé={finishScore}, Distance={distanceScore}, Temps={timeScore}, TOTAL={score.totalScore}");
        }

        // Déclenche l'événement seulement si le score a changé
        if (score.totalScore != oldScore)
        {
            OnScoreChanged?.Invoke(playerID, score.totalScore);
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
                Debug.Log("ScoreManager: Tous les joueurs ont terminé! Envoi des scores...");
            }

            hasEndedGame = true;
            SendScoresToAPI();
#if !WEB_BUILD
            StartCoroutine(SendScoresToArcade());
#endif
        }
    }

    /// <summary>
    /// Sauvegarde les scores manuellement (appelé au Quit)
    /// Envoie les scores même si tous les joueurs n'ont pas terminé
    /// </summary>
    public void SaveScoresNow(System.Action<bool> onComplete = null)
    {
        // Envoie les scores seulement s'ils n'ont pas déjà été envoyés
        if (!hasEndedGame)
        {
            hasEndedGame = true; // Marque comme envoyé pour éviter les doublons
            SendScoresToAPI(onComplete);
#if !WEB_BUILD
            StartCoroutine(SendScoresToArcade());
#endif
        }
        else
        {
            if (showDebug)
            {
                Debug.Log("ScoreManager: Scores déjà envoyés, skip.");
            }
            onComplete?.Invoke(true);
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
    /// Envoie tous les scores à l'API via GameSessionManager (persistant)
    /// </summary>
    private void SendScoresToAPI(System.Action<bool> onComplete = null)
    {
        if (GameSessionManager.Instance == null)
        {
            onComplete?.Invoke(false);
            return;
        }

        // In WEB_BUILD multiplayer each client only has accurate data for ITS OWN player —
        // the opponent's score is whatever the local ScoreManager happened to update from
        // network events, which is incomplete. Both clients POST in parallel; each contributes
        // its own row to /api/game/end so the final DB has one accurate record per player.
#if WEB_BUILD
        int localPlayerNumber = GameSessionManager.Instance.LocalPlayerNumber;
        if (localPlayerNumber <= 0)
        {
            if (showDebug) Debug.LogWarning("ScoreManager: LocalPlayerNumber unknown, cannot send scores");
            onComplete?.Invoke(false);
            return;
        }
#endif

        // Prépare les données des scores
        List<GameSessionManager.PlayerScoreData> scoresList = new List<GameSessionManager.PlayerScoreData>();

        foreach (var kvp in playerScores)
        {
            PlayerScore score = kvp.Value;
#if WEB_BUILD
            if (score.playerID != localPlayerNumber) continue;
#endif
            scoresList.Add(new GameSessionManager.PlayerScoreData
            {
                playerNumber = score.playerID,
                totalScore = score.totalScore,
                distanceTraveled = score.distanceTraveled,
                survivalTime = score.survivalTime,
                collectiblesCollected = score.orbsCollected,    // Orbes de lumière
                perfectJumps = score.perfectJumps,              // Sauts parfaits
                hasFinished = score.hasFinished
            });
        }

        if (scoresList.Count == 0)
        {
            if (showDebug) Debug.LogWarning("ScoreManager: no scores to send");
            onComplete?.Invoke(false);
            return;
        }

        GameSessionManager.Instance.SendScores(scoresList.ToArray(), onComplete);
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
