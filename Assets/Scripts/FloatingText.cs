using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Crée un texte qui flotte et disparaît (feedback visuel de collecte)
/// Usage: FloatingText.Create("+10", transform.position, Color.yellow);
/// </summary>
[RequireComponent(typeof(TextMeshPro))]
public class FloatingText : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Vitesse de montée du texte")]
    public float riseSpeed = 2f;

    [Tooltip("Durée de vie du texte")]
    public float lifetime = 1.5f;

    [Tooltip("Décalage latéral aléatoire")]
    public float randomOffset = 0.5f;

    [Tooltip("Scale initial du texte")]
    public float startScale = 1f;

    [Tooltip("Scale final du texte")]
    public float endScale = 1.5f;

    private TextMeshPro textMesh;
    private Color originalColor;
    private Vector3 moveDirection;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        if (textMesh != null)
        {
            originalColor = textMesh.color;
        }
    }

    void Start()
    {
        // Direction aléatoire légère
        float randomX = Random.Range(-randomOffset, randomOffset);
        moveDirection = new Vector3(randomX, 1f, 0f).normalized;

        // Lance l'animation
        StartCoroutine(FloatAndFade());
    }

    private IEnumerator FloatAndFade()
    {
        float elapsed = 0f;
        Vector3 startPosition = transform.position;

        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / lifetime;

            // Mouvement vers le haut
            transform.position = startPosition + moveDirection * (riseSpeed * elapsed);

            // Agrandissement progressif
            float currentScale = Mathf.Lerp(startScale, endScale, progress);
            transform.localScale = Vector3.one * currentScale;

            // Fade out
            if (textMesh != null)
            {
                Color color = originalColor;
                color.a = Mathf.Lerp(1f, 0f, progress);
                textMesh.color = color;
            }

            yield return null;
        }

        // Détruit l'objet
        Destroy(gameObject);
    }

    /// <summary>
    /// Crée un texte flottant à une position donnée
    /// </summary>
    /// <param name="text">Le texte à afficher</param>
    /// <param name="position">Position de spawn</param>
    /// <param name="color">Couleur du texte</param>
    /// <returns>L'instance du FloatingText créé</returns>
    public static FloatingText Create(string text, Vector3 position, Color color)
    {
        // Crée un GameObject avec TextMeshPro
        GameObject obj = new GameObject("FloatingText");
        obj.transform.position = position;

        // Ajoute TextMeshPro
        TextMeshPro textMesh = obj.AddComponent<TextMeshPro>();
        textMesh.text = text;
        textMesh.fontSize = 5;
        textMesh.color = color;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.fontStyle = FontStyles.Bold;

        // Ajoute le script FloatingText
        FloatingText floatingText = obj.AddComponent<FloatingText>();

        return floatingText;
    }

    /// <summary>
    /// Crée un texte flottant avec une valeur numérique (pour les scores)
    /// </summary>
    public static FloatingText CreateScore(int value, Vector3 position)
    {
        string text = value > 0 ? $"+{value}" : value.ToString();
        Color color = value > 0 ? Color.yellow : Color.red;
        return Create(text, position, color);
    }
}
