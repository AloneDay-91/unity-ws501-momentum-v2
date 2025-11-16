using UnityEngine;

/// <summary>
/// Fait pulser un objet (changement de scale) pour le rendre plus vivant
/// Parfait pour les orbes, power-ups, objets collectables
/// </summary>
public class ObjectPulse : MonoBehaviour
{
    [Header("Pulse Settings")]
    [Tooltip("Scale minimum (1 = taille normale)")]
    [Range(0.8f, 1f)]
    public float minScale = 0.9f;

    [Tooltip("Scale maximum")]
    [Range(1f, 1.5f)]
    public float maxScale = 1.1f;

    [Tooltip("Vitesse de la pulsation")]
    [Range(0.5f, 5f)]
    public float pulseSpeed = 2f;

    [Header("Rotation (optionnel)")]
    [Tooltip("Activer la rotation")]
    public bool enableRotation = true;

    [Tooltip("Vitesse de rotation (degrés/seconde)")]
    public Vector3 rotationSpeed = new Vector3(0, 30, 0);

    [Header("Float (optionnel)")]
    [Tooltip("Activer le mouvement de flottement")]
    public bool enableFloat = true;

    [Tooltip("Amplitude du flottement")]
    [Range(0f, 1f)]
    public float floatAmplitude = 0.2f;

    [Tooltip("Vitesse du flottement")]
    [Range(0.5f, 3f)]
    public float floatSpeed = 1f;

    private Vector3 originalScale;
    private Vector3 originalPosition;
    private float timeOffset;

    void Start()
    {
        originalScale = transform.localScale;
        originalPosition = transform.position;

        // Offset aléatoire pour que tous les orbes ne pulsent pas en même temps
        timeOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        float time = Time.time + timeOffset;

        // Pulsation de la taille
        float scaleValue = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(time * pulseSpeed) + 1f) / 2f);
        transform.localScale = originalScale * scaleValue;

        // Rotation
        if (enableRotation)
        {
            transform.Rotate(rotationSpeed * Time.deltaTime, Space.World);
        }

        // Flottement vertical
        if (enableFloat)
        {
            float yOffset = Mathf.Sin(time * floatSpeed) * floatAmplitude;
            transform.position = originalPosition + new Vector3(0, yOffset, 0);
        }
    }

    /// <summary>
    /// Met à jour la position d'origine (à appeler si l'objet est déplacé)
    /// </summary>
    public void UpdateOriginalPosition()
    {
        originalPosition = transform.position;
    }

    /// <summary>
    /// Pulse rapide unique (pour feedback de collecte)
    /// </summary>
    public void PulseOnce(float intensity = 1.5f, float duration = 0.2f)
    {
        StartCoroutine(PulseOnceCoroutine(intensity, duration));
    }

    private System.Collections.IEnumerator PulseOnceCoroutine(float intensity, float duration)
    {
        Vector3 targetScale = originalScale * intensity;
        float elapsed = 0f;

        // Grossit
        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (duration / 2f);
            transform.localScale = Vector3.Lerp(originalScale, targetScale, progress);
            yield return null;
        }

        elapsed = 0f;

        // Revient à la normale
        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (duration / 2f);
            transform.localScale = Vector3.Lerp(targetScale, originalScale, progress);
            yield return null;
        }

        transform.localScale = originalScale;
    }
}
