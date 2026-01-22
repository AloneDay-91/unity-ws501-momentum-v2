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

    [Header("Player 1 UI")]
    [Tooltip("Texte distance pour Joueur 1")]
    public TMP_Text p1DistanceText;
    [Tooltip("Texte rang pour Joueur 1")]
    public TMP_Text p1RankText;

    [Header("Player 2 UI")]
    [Tooltip("Texte distance pour Joueur 2")]
    public TMP_Text p2DistanceText;
    [Tooltip("Texte rang pour Joueur 2")]
    public TMP_Text p2RankText;

    [Header("Format")]
    [Tooltip("Format du texte de distance")]
    public string distanceFormat = "{0:F0}m";

    [Tooltip("Format du texte de classement")]
    public string rankFormat = "{0}/{1}";

    [Header("Laser Wall Indicator")]
    [Tooltip("Indicateur du mur de laser")]
    public Image laserWallIndicator;

    [Tooltip("Couleur de l'indicateur du mur")]
    public Color laserWallColor = Color.red;

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

        // Debug: Affiche les infos de configuration
        Debug.Log($"ProgressionUI: Initialisé avec {playerIndicators.Count} joueurs détectés");
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
        
        // Mise à jour des textes P1 et P2
        UpdatePlayerUI(1, p1DistanceText, p1RankText);
        UpdatePlayerUI(2, p2DistanceText, p2RankText);
    }

    private void UpdatePlayerUI(int playerId, TMP_Text distText, TMP_Text rkText)
    {
        Transform playerTransform = GetPlayerTransformByID(playerId);
        
        // Mise à jour Distance
        if (distText != null)
        {
            if (playerTransform != null)
            {
                float distance = progressionTracker.GetDistanceTraveled(playerTransform);
                distText.text = string.Format(distanceFormat, distance);
            }
            else
            {
                distText.text = "";
            }
        }

        // Mise à jour Rank
        if (rkText != null)
        {
            if (playerTransform != null)
            {
                int rank = progressionTracker.GetPlayerRank(playerTransform);
                int total = progressionTracker.players.Count;
                rkText.text = string.Format(rankFormat, rank, total);
            }
            else
            {
                rkText.text = "";
            }
        }
    }

    private Transform GetPlayerTransformByID(int playerId)
    {
        foreach (Transform p in progressionTracker.players)
        {
            if (p == null) continue;
            var input = p.GetComponent<PlayerInput>();
            if (input != null && input.playerID == playerId) return p;
        }
        return null;
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
        }
    }

    private void UpdateLaserWallIndicator()
    {
        if (laserWallIndicator == null || indicatorsParent == null) return;

        float progress = progressionTracker.GetLaserWallProgress();
        float barWidth = indicatorsParent.rect.width;

        RectTransform wallRect = laserWallIndicator.rectTransform;
        float xPos = progress * barWidth;
        wallRect.anchoredPosition = new Vector2(xPos, 0);
    }
}
