using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Barre de progression qui affiche le nombre d'orbs collectés
/// Utilise une Image UI avec fillAmount pour animer la progression
/// </summary>
public class OrbCollectionBar : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("L'image de la barre (fill) - l'image jaune/verte")]
    public Image fillBar;

    [Tooltip("Texte affichant le nombre d'orbs (ex: '5/10')")]
    public TextMeshProUGUI orbCountText;

    [Header("Settings")]
    [Tooltip("Nombre d'orbs nécessaires pour remplir la barre")]
    public int maxOrbs = 10;

    [Tooltip("Vitesse d'animation de la barre")]
    [Range(1f, 20f)]
    public float fillSpeed = 5f;

    [Header("Colors (optionnel)")]
    [Tooltip("Couleur de la barre quand vide/peu remplie")]
    public Color lowColor = Color.red;

    [Tooltip("Couleur de la barre quand moyennement remplie")]
    public Color mediumColor = Color.yellow;

    [Tooltip("Couleur de la barre quand pleine")]
    public Color fullColor = Color.green;

    [Tooltip("Activer le changement de couleur selon progression")]
    public bool enableColorGradient = false;

    private int currentOrbs = 0;
    private float targetFillAmount = 0f;

    void Start()
    {
        if (fillBar != null)
        {
            fillBar.fillAmount = 0f;
        }
        UpdateUI();
    }

    void Update()
    {
        // Animation smooth de la barre
        if (fillBar != null && fillBar.fillAmount != targetFillAmount)
        {
            fillBar.fillAmount = Mathf.Lerp(fillBar.fillAmount, targetFillAmount, fillSpeed * Time.deltaTime);

            // Change la couleur selon la progression
            if (enableColorGradient)
            {
                UpdateBarColor();
            }
        }
    }

    /// <summary>
    /// Ajoute des orbs à la barre
    /// </summary>
    public void AddOrbs(int amount)
    {
        currentOrbs = Mathf.Clamp(currentOrbs + amount, 0, maxOrbs);
        targetFillAmount = (float)currentOrbs / maxOrbs;
        UpdateUI();

        // Si la barre est pleine, déclenche un événement
        if (currentOrbs >= maxOrbs)
        {
            OnBarFull();
        }
    }

    /// <summary>
    /// Définit le nombre d'orbs directement
    /// </summary>
    public void SetOrbs(int amount)
    {
        currentOrbs = Mathf.Clamp(amount, 0, maxOrbs);
        targetFillAmount = (float)currentOrbs / maxOrbs;
        UpdateUI();
    }

    /// <summary>
    /// Réinitialise la barre à zéro
    /// </summary>
    public void ResetBar()
    {
        currentOrbs = 0;
        targetFillAmount = 0f;
        if (fillBar != null)
        {
            fillBar.fillAmount = 0f;
        }
        UpdateUI();
    }

    /// <summary>
    /// Met à jour le texte et l'affichage
    /// </summary>
    private void UpdateUI()
    {
        if (orbCountText != null)
        {
            orbCountText.text = $"{currentOrbs}/{maxOrbs}";
        }
    }

    /// <summary>
    /// Met à jour la couleur de la barre selon la progression
    /// </summary>
    private void UpdateBarColor()
    {
        if (fillBar == null) return;

        float fill = fillBar.fillAmount;

        if (fill < 0.33f)
        {
            // Rouge à jaune
            fillBar.color = Color.Lerp(lowColor, mediumColor, fill * 3f);
        }
        else if (fill < 0.66f)
        {
            // Jaune à vert
            fillBar.color = Color.Lerp(mediumColor, fullColor, (fill - 0.33f) * 3f);
        }
        else
        {
            // Vert
            fillBar.color = fullColor;
        }
    }

    /// <summary>
    /// Appelé quand la barre est pleine
    /// </summary>
    private void OnBarFull()
    {
        Debug.Log("🎉 Barre d'orbs pleine!");

        // Tu peux ajouter des effets ici :
        // - Shake de la barre
        // - Son de victoire
        // - Bonus de score
        // - etc.

        // Exemple : shake de caméra
        if (CameraShakeManager.Instance != null)
        {
            CameraShakeManager.Instance.ShakeAllMedium();
        }
    }

    /// <summary>
    /// Retourne le nombre actuel d'orbs
    /// </summary>
    public int GetCurrentOrbs()
    {
        return currentOrbs;
    }

    /// <summary>
    /// Retourne vrai si la barre est pleine
    /// </summary>
    public bool IsFull()
    {
        return currentOrbs >= maxOrbs;
    }

    /// <summary>
    /// Retourne le pourcentage de remplissage (0 à 1)
    /// </summary>
    public float GetFillPercentage()
    {
        return (float)currentOrbs / maxOrbs;
    }
}
