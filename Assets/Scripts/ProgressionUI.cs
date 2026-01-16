using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Affiche la progression des joueurs dans l'UI
/// Attache ce script à un GameObject UI (ex: Canvas/ProgressionUI)
/// </summary>
public class ProgressionUI : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Référence au ProgressionTracker")]
    public ProgressionTracker progressionTracker;

    [Header("Progress Bar")]
    [Tooltip("Image de la barre de progression")]
    public Image progressBarFill;

    [Tooltip("Couleur de la barre")]
    public Color progressBarColor = Color.cyan;

    [Header("Player Indicators")]
    [Tooltip("Prefab de l'indicateur de joueur (Image avec un Icon)")]
    public GameObject playerIndicatorPrefab;

    [Tooltip("Parent des indicateurs de joueurs")]
    public RectTransform indicatorsParent;

    [Tooltip("Couleurs des indicateurs par joueur")]
    public Color[] playerColors = new Color[] { Color.blue, Color.red, Color.green, Color.yellow };

    [Header("Laser Wall Indicator")]
    [Tooltip("Indicateur du mur de laser")]
    public Image laserWallIndicator;

    [Tooltip("Couleur de l'indicateur du mur")]
    public Color laserWallColor = Color.red;

    [Header("Distance Display")]
    [Tooltip("Texte pour afficher la distance avec le mur")]
    public TMP_Text distanceText;

    [Tooltip("Format du texte de distance")]
    public string distanceFormat = "Distance: {0:F0}m";

    [Header("Rank Display")]
    [Tooltip("Texte pour afficher le classement")]
    public TMP_Text rankText;

    [Tooltip("Format du texte de classement")]
    public string rankFormat = "Rank: {0}/{1}";

    [Header("Settings")]
    [Tooltip("Afficher le joueur local seulement (pour split screen)")]
    public bool showLocalPlayerOnly = false;

    [Tooltip("ID du joueur local (1 ou 2)")]
    public int localPlayerID = 1;

    [Tooltip("Afficher le mur de laser")]
    public bool showLaserWall = true;

    // Indicateurs créés dynamiquement
    private Dictionary<Transform, Image> playerIndicators = new Dictionary<Transform, Image>();
    private CanvasGroup canvasGroup;

    void Start()
    {
        // Récupère ou ajoute le CanvasGroup
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Trouve le tracker si pas assigné
        if (progressionTracker == null)
        {
            progressionTracker = ProgressionTracker.Instance;
        }

        if (progressionTracker == null)
        {
            Debug.LogWarning("ProgressionUI: ProgressionTracker introuvable !");
            return;
        }

        // Configure la barre de progression
        if (progressBarFill != null)
        {
            progressBarFill.color = progressBarColor;
            progressBarFill.fillAmount = 0f;
        }

        // Configure l'indicateur du mur
        if (laserWallIndicator != null)
        {
            laserWallIndicator.color = laserWallColor;
        }

        // Crée les indicateurs de joueurs
        CreatePlayerIndicators();

        // Cache l'UI au démarrage si countdown est actif
        if (ShouldHideUI())
        {
            HideUI();
        }

        // Debug: Affiche les infos de configuration
        Debug.Log($"ProgressionUI: Initialisé avec {playerIndicators.Count} joueurs détectés");
        if (progressBarFill != null)
            Debug.Log($"ProgressionUI: Progress bar trouvée");
        if (distanceText != null)
            Debug.Log($"ProgressionUI: Distance text trouvé");
        if (rankText != null)
            Debug.Log($"ProgressionUI: Rank text trouvé");
    }

    void Update()
    {
        if (progressionTracker == null) return;

        // Vérifie si on a besoin de créer les indicateurs (problème d'ordre d'exécution)
        if (playerIndicators.Count == 0 && progressionTracker.players.Count > 0)
        {
            Debug.Log($"ProgressionUI: Recréation des indicateurs - {progressionTracker.players.Count} joueurs trouvés");
            CreatePlayerIndicators();
        }

        // Debug périodique de l'état du CanvasGroup
        if (Time.frameCount % 120 == 0)
        {
            if (canvasGroup != null)
            {
                Debug.Log($"ProgressionUI: CanvasGroup alpha={canvasGroup.alpha}");
            }
            bool shouldHide = ShouldHideUI();
            Debug.Log($"ProgressionUI: ShouldHideUI={shouldHide}");
        }

        // Hide UI when countdown overlay is active
        if (ShouldHideUI())
        {
            HideUI();
            return;
        }
        else
        {
            ShowUI();
        }

        UpdatePlayerIndicators();
        UpdateLaserWallIndicator();
        UpdateDistanceText();
        UpdateRankText();
    }

    private bool ShouldHideUI()
    {
        // Check if GameManager exists and countdown overlay or game over panel is active
        if (GameManager.Instance != null)
        {
            bool countdownActive = GameManager.Instance.countdownOverlay != null &&
                                   GameManager.Instance.countdownOverlay.activeSelf;
            bool gameOverActive = GameManager.Instance.gameOverPanel != null &&
                                 GameManager.Instance.gameOverPanel.activeSelf;

            return countdownActive || gameOverActive;
        }
        return false;
    }

    private void HideUI()
    {
        // Cache l'UI en utilisant CanvasGroup (le GameObject reste actif)
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void ShowUI()
    {
        // Affiche l'UI en utilisant CanvasGroup
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    private void CreatePlayerIndicators()
    {
        if (playerIndicatorPrefab == null || indicatorsParent == null) return;

        foreach (Transform player in progressionTracker.players)
        {
            if (player == null) continue;

            // Filtre par joueur local si nécessaire
            if (showLocalPlayerOnly)
            {
                PlayerInput playerInput = player.GetComponent<PlayerInput>();
                if (playerInput != null && playerInput.playerID != localPlayerID)
                {
                    continue;
                }
            }

            // Crée l'indicateur
            GameObject indicator = Instantiate(playerIndicatorPrefab, indicatorsParent);
            Image indicatorImage = indicator.GetComponent<Image>();

            if (indicatorImage != null)
            {
                // Assigne une couleur unique
                PlayerInput playerInput = player.GetComponent<PlayerInput>();
                int colorIndex = playerInput != null ? playerInput.playerID - 1 : 0;
                colorIndex = Mathf.Clamp(colorIndex, 0, playerColors.Length - 1);
                indicatorImage.color = playerColors[colorIndex];

                playerIndicators[player] = indicatorImage;
            }
        }
    }

    private void UpdatePlayerIndicators()
    {
        if (indicatorsParent == null) return;

        float barWidth = indicatorsParent.rect.width;

        foreach (var kvp in playerIndicators)
        {
            Transform player = kvp.Key;
            Image indicator = kvp.Value;

            if (player == null || indicator == null) continue;

            // Calcule la progression
            float progress = progressionTracker.GetPlayerProgress(player);

            // Positionne l'indicateur
            RectTransform indicatorRect = indicator.rectTransform;
            float xPos = progress * barWidth;
            indicatorRect.anchoredPosition = new Vector2(xPos, 0);

            // Debug occasionnel
            if (Time.frameCount % 60 == 0) // Toutes les 60 frames
            {
                Debug.Log($"Player {player.name}: position={player.position.x:F1}, progress={progress:F2}, xPos={xPos:F1}");
            }
        }

        // Update progress bar fill
        if (progressBarFill != null && playerIndicators.Count > 0)
        {
            // Utilise le joueur le plus avancé pour la barre
            float maxProgress = 0f;
            foreach (var kvp in playerIndicators)
            {
                if (kvp.Key != null)
                {
                    float progress = progressionTracker.GetPlayerProgress(kvp.Key);
                    maxProgress = Mathf.Max(maxProgress, progress);
                }
            }
            progressBarFill.fillAmount = maxProgress;

            // Debug occasionnel
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"ProgressionUI: Progress bar fillAmount set to {maxProgress:F2} ({maxProgress * 100:F0}%)");
            }
        }
        else
        {
            if (Time.frameCount % 60 == 0)
            {
                Debug.LogWarning($"ProgressionUI: Cannot update progress bar - progressBarFill={progressBarFill != null}, playerIndicators.Count={playerIndicators.Count}");
            }
        }
    }

    private void UpdateLaserWallIndicator()
    {
        if (!showLaserWall || laserWallIndicator == null || indicatorsParent == null) return;

        float progress = progressionTracker.GetLaserWallProgress();
        float barWidth = indicatorsParent.rect.width;

        RectTransform wallRect = laserWallIndicator.rectTransform;
        float xPos = progress * barWidth;
        wallRect.anchoredPosition = new Vector2(xPos, 0);
    }

    private void UpdateDistanceText()
    {
        if (distanceText == null || progressionTracker == null) return;

        // Trouve le joueur local
        Transform localPlayer = GetLocalPlayer();
        if (localPlayer == null)
        {
            if (Time.frameCount % 60 == 0)
            {
                Debug.LogWarning("ProgressionUI: Aucun joueur local trouvé pour afficher la distance");
            }
            return;
        }

        float distance = progressionTracker.GetDistanceFromLaserWall(localPlayer);

        if (distance == float.MaxValue)
        {
            distanceText.text = "";
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log("ProgressionUI: Distance = MaxValue (pas de laser wall?)");
            }
            return;
        }

        distanceText.text = string.Format(distanceFormat, distance);

        // Debug occasionnel
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"ProgressionUI: Distance text updated: {distance:F1}m");
        }

        // Change la couleur selon la distance
        if (progressionTracker.IsPlayerInDanger(localPlayer, 15f))
        {
            distanceText.color = Color.red;
        }
        else if (distance < 30f)
        {
            distanceText.color = Color.yellow;
        }
        else
        {
            distanceText.color = Color.white;
        }
    }

    private void UpdateRankText()
    {
        if (rankText == null || progressionTracker == null) return;

        // Trouve le joueur local
        Transform localPlayer = GetLocalPlayer();
        if (localPlayer == null) return;

        int rank = progressionTracker.GetPlayerRank(localPlayer);
        int totalPlayers = progressionTracker.players.Count;

        rankText.text = string.Format(rankFormat, rank, totalPlayers);
    }

    private Transform GetLocalPlayer()
    {
        if (progressionTracker == null) return null;

        if (showLocalPlayerOnly)
        {
            // Trouve le joueur local par ID
            foreach (Transform player in progressionTracker.players)
            {
                if (player == null) continue;

                PlayerInput playerInput = player.GetComponent<PlayerInput>();
                if (playerInput != null && playerInput.playerID == localPlayerID)
                {
                    return player;
                }
            }
        }
        else
        {
            // Retourne le premier joueur par défaut
            return progressionTracker.players.Count > 0 ? progressionTracker.players[0] : null;
        }

        return null;
    }

    /// <summary>
    /// Met à jour l'ID du joueur local (pour changer de vue)
    /// </summary>
    public void SetLocalPlayer(int playerID)
    {
        localPlayerID = playerID;

        // Recrée les indicateurs
        foreach (var indicator in playerIndicators.Values)
        {
            if (indicator != null)
            {
                Destroy(indicator.gameObject);
            }
        }
        playerIndicators.Clear();
        CreatePlayerIndicators();
    }
}
