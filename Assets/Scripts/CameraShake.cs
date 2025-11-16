using UnityEngine;
using System.Collections;

/// <summary>
/// Gère le tremblement de la caméra pour plus d'impact visuel
/// Usage: CameraShake.Instance.Shake(0.3f, 0.2f);
/// </summary>
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("Shake Settings")]
    [Tooltip("Multiplicateur global de l'intensité du shake")]
    [Range(0f, 2f)]
    public float shakeMultiplier = 1f;

    private Vector3 originalPosition;
    private Coroutine shakeCoroutine;

    void Awake()
    {
        // Singleton optionnel (pour compatibilité avec ancien code)
        // Si plusieurs caméras, seule la première sera Instance
        if (Instance == null)
        {
            Instance = this;
        }
    }

    void Start()
    {
        originalPosition = transform.localPosition;
    }

    /// <summary>
    /// Déclenche un shake de caméra
    /// </summary>
    /// <param name="duration">Durée du shake en secondes</param>
    /// <param name="intensity">Intensité du shake (0.1 = léger, 0.5 = fort)</param>
    public void Shake(float duration, float intensity)
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }
        shakeCoroutine = StartCoroutine(ShakeCoroutine(duration, intensity * shakeMultiplier));
    }

    /// <summary>
    /// Shake rapide pour feedback léger (collecte, etc.)
    /// </summary>
    public void ShakeLight()
    {
        Shake(0.15f, 0.1f);
    }

    /// <summary>
    /// Shake moyen pour événements importants
    /// </summary>
    public void ShakeMedium()
    {
        Shake(0.25f, 0.25f);
    }

    /// <summary>
    /// Shake fort pour événements majeurs (transition jour/nuit)
    /// </summary>
    public void ShakeStrong()
    {
        Shake(0.4f, 0.5f);
    }

    private IEnumerator ShakeCoroutine(float duration, float intensity)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Génère une position aléatoire dans un cercle
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;

            transform.localPosition = originalPosition + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;

            // Diminution progressive de l'intensité
            float percentComplete = elapsed / duration;
            intensity = Mathf.Lerp(intensity, 0, percentComplete);

            yield return null;
        }

        // Retour à la position d'origine
        transform.localPosition = originalPosition;
        shakeCoroutine = null;
    }

    /// <summary>
    /// Met à jour la position d'origine (à appeler si la caméra se déplace)
    /// </summary>
    public void UpdateOriginalPosition()
    {
        if (shakeCoroutine == null)
        {
            originalPosition = transform.localPosition;
        }
    }
}
