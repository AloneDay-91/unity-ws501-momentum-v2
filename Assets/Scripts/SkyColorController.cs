using UnityEngine;
using System.Collections; // Important pour utiliser les Coroutines

[RequireComponent(typeof(Camera))]
public class SkyColorController : MonoBehaviour
{
    [Header("Couleurs du Cycle")]
    public Color dayColor = new Color(0.2f, 0.7f, 1.0f); // (Bleu ciel)
    public Color nightColor = new Color(0.1f, 0.0f, 0.2f); // (Violet foncé)

    [Header("Configuration du Fondu")]
    [Tooltip("Durée (en secondes) du fondu entre le jour et la nuit.")]
    public float fadeDuration = 2.0f; // Durée du fondu

    private Camera cam;
    private Coroutine currentFadeCoroutine; // Référence à la coroutine en cours

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    // On s'abonne aux événements
    void OnEnable()
    {
        GameCycleManager.OnDayStart += SetDayColor;
        GameCycleManager.OnNightStart += SetNightColor;
    }

    // On se désabonne
    void OnDisable()
    {
        GameCycleManager.OnDayStart -= SetDayColor;
        GameCycleManager.OnNightStart -= SetNightColor;
    }

    // Gère l'état au démarrage (instantané)
    void Start()
    {
        if (GameCycleManager.Instance.IsDay)
        {
            cam.backgroundColor = dayColor;
        }
        else
        {
            cam.backgroundColor = nightColor;
        }
    }

    // --- FONCTIONS MODIFIÉES ---

    void SetDayColor()
    {
        // On lance la coroutine de fondu vers la couleur "Jour"
        StartFade(dayColor);
    }

    void SetNightColor()
    {
        // On lance la coroutine de fondu vers la couleur "Nuit"
        StartFade(nightColor);
    }

    // --- NOUVELLES FONCTIONS ---

    // Gère le démarrage et l'arrêt de la coroutine
    void StartFade(Color targetColor)
    {
        // Si un fondu est déjà en cours, on l'arrête
        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
        }
        
        // On lance le nouveau fondu
        currentFadeCoroutine = StartCoroutine(FadeToColor(targetColor));
    }

    // La coroutine qui fait le fondu
    IEnumerator FadeToColor(Color targetColor)
    {
        Color startColor = cam.backgroundColor; // Couleur de départ
        float timer = 0f;

        // Boucle tant que le fondu n'est pas terminé
        while (timer < fadeDuration)
        {
            // Avance le minuteur
            timer += Time.deltaTime;
            
            // Calcule le pourcentage d'avancement (de 0.0 à 1.0)
            float normalizedTime = timer / fadeDuration;
            
            // Applique la couleur intermédiaire
            cam.backgroundColor = Color.Lerp(startColor, targetColor, normalizedTime);
            
            // Attend la prochaine image
            yield return null; 
        }

        // S'assure que la couleur finale est exacte
        cam.backgroundColor = targetColor;
        currentFadeCoroutine = null; // Marque le fondu comme terminé
    }
}