using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Barre de luminescence segmentée (rectangles qui se remplissent un par un)
/// Style : plusieurs petits rectangles côte à côte
/// </summary>
public class SegmentedLuminescenceBar : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Quel joueur cette barre doit-elle suivre ? (1 ou 2)")]
    public int playerIDToTrack = 1;

    [Header("Segment Settings")]
    [Tooltip("Nombre de segments dans la barre")]
    [Range(3, 20)]
    public int segmentCount = 10;

    [Tooltip("Sprite pour segment plein (jaune/vert)")]
    public Sprite fullSegmentSprite;

    [Tooltip("Sprite pour segment vide (gris/noir)")]
    public Sprite emptySegmentSprite;

    [Tooltip("Espacement entre les segments")]
    [Range(0f, 20f)]
    public float segmentSpacing = 5f;

    [Tooltip("Largeur de chaque segment")]
    public float segmentWidth = 30f;

    [Tooltip("Hauteur de chaque segment")]
    public float segmentHeight = 40f;

    [Header("Colors (optionnel)")]
    [Tooltip("Couleur pour segment plein")]
    public Color fullColor = Color.white;

    [Tooltip("Couleur pour segment vide")]
    public Color emptyColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    [Header("Animation")]
    [Tooltip("Animer le changement de segments")]
    public bool animateSegments = true;

    [Tooltip("Vitesse d'animation (lerp)")]
    [Range(1f, 20f)]
    public float animationSpeed = 10f;

    private PlayerStats targetPlayerStats;
    private List<Image> segments = new List<Image>();
    private int currentFilledSegments = 0;
    private int targetFilledSegments = 0;

    void Start()
    {
        // Trouve le bon script PlayerStats à suivre
        PlayerStats[] allPlayers = FindObjectsOfType<PlayerStats>();
        foreach (PlayerStats player in allPlayers)
        {
            if (player.GetComponent<PlayerInput>().playerID == playerIDToTrack)
            {
                targetPlayerStats = player;
                break;
            }
        }

        // Génère les segments
        GenerateSegments();
    }

    void Update()
    {
        if (targetPlayerStats != null)
        {
            // Calcule combien de segments devraient être pleins
            float fillPercentage = targetPlayerStats.currentLuminescence / targetPlayerStats.maxLuminescence;
            targetFilledSegments = Mathf.FloorToInt(fillPercentage * segmentCount);

            // Animation ou changement direct
            if (animateSegments)
            {
                // Animation progressive
                if (currentFilledSegments < targetFilledSegments)
                {
                    currentFilledSegments = Mathf.Min(currentFilledSegments + 1, targetFilledSegments);
                }
                else if (currentFilledSegments > targetFilledSegments)
                {
                    currentFilledSegments = Mathf.Max(currentFilledSegments - 1, targetFilledSegments);
                }
            }
            else
            {
                // Changement instantané
                currentFilledSegments = targetFilledSegments;
            }

            // Met à jour l'affichage des segments
            UpdateSegments();
        }
    }

    /// <summary>
    /// Génère les segments UI
    /// </summary>
    private void GenerateSegments()
    {
        // Nettoie les anciens segments si on regénère
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        segments.Clear();

        // Crée les nouveaux segments
        for (int i = 0; i < segmentCount; i++)
        {
            // Crée un GameObject pour le segment
            GameObject segmentObj = new GameObject($"Segment_{i}");
            segmentObj.transform.SetParent(transform, false);

            // Ajoute le composant Image
            Image segmentImage = segmentObj.AddComponent<Image>();
            segmentImage.sprite = emptySegmentSprite;
            segmentImage.color = emptyColor;

            // Configure la taille et position
            RectTransform rectTransform = segmentObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(segmentWidth, segmentHeight);

            // Position horizontale (de gauche à droite)
            float xPos = i * (segmentWidth + segmentSpacing);
            rectTransform.anchoredPosition = new Vector2(xPos, 0);
            rectTransform.anchorMin = new Vector2(0, 0.5f);
            rectTransform.anchorMax = new Vector2(0, 0.5f);
            rectTransform.pivot = new Vector2(0, 0.5f);

            segments.Add(segmentImage);
        }

        // Ajuste la taille du conteneur parent
        RectTransform parentRect = GetComponent<RectTransform>();
        if (parentRect != null)
        {
            float totalWidth = (segmentCount * segmentWidth) + ((segmentCount - 1) * segmentSpacing);
            parentRect.sizeDelta = new Vector2(totalWidth, segmentHeight);
        }
    }

    /// <summary>
    /// Met à jour l'affichage des segments
    /// </summary>
    private void UpdateSegments()
    {
        for (int i = 0; i < segments.Count; i++)
        {
            if (i < currentFilledSegments)
            {
                // Segment plein
                segments[i].sprite = fullSegmentSprite;
                segments[i].color = fullColor;
            }
            else
            {
                // Segment vide
                segments[i].sprite = emptySegmentSprite;
                segments[i].color = emptyColor;
            }
        }
    }

    /// <summary>
    /// Change le nombre de segments (utile pour ajuster en temps réel)
    /// </summary>
    public void SetSegmentCount(int count)
    {
        segmentCount = Mathf.Clamp(count, 3, 20);
        GenerateSegments();
    }

    /// <summary>
    /// Régénère les segments (si tu changes les sprites en runtime)
    /// </summary>
    public void RefreshSegments()
    {
        GenerateSegments();
    }

    // Visualisation dans l'éditeur
    void OnValidate()
    {
        // Regénère les segments si on change les paramètres dans l'éditeur
        // (Seulement si on est en play mode)
        if (Application.isPlaying && segments.Count != segmentCount)
        {
            GenerateSegments();
        }
    }
}
