using UnityEngine;
using UnityEngine.Rendering; // Important pour le Post-Processing
using UnityEngine.Rendering.Universal; // Important pour URP
using System.Collections;

[RequireComponent(typeof(Volume))]
public class NightVignetteController : MonoBehaviour
{
    [Header("Réglages de la Vignette")]
    [Tooltip("L'intensité de la vignette pendant la nuit (0 = rien, 1 = max).")]
    public float nightIntensity = 0.8f; // 0.8 est un bon début
    [Tooltip("La durée du fondu (doit être la même que les autres).")]
    public float fadeDuration = 2.0f;

    private Volume postProcessVolume;
    private Vignette vignette;
    private Coroutine currentFadeCoroutine;

    void Awake()
    {
        postProcessVolume = GetComponent<Volume>();
        
        // Tente de trouver la Vignette dans le profil
        postProcessVolume.profile.TryGet(out vignette);
    }

    // On s'abonne aux événements
    void OnEnable()
    {
        GameCycleManager.OnDayStart += GoToDay;
        GameCycleManager.OnNightStart += GoToNight;
    }

    void OnDisable()
    {
        GameCycleManager.OnDayStart -= GoToDay;
        GameCycleManager.OnNightStart -= GoToNight;
    }

    // Gère l'état au démarrage
    void Start()
    {
        if (vignette == null)
        {
            Debug.LogError("Vignette non trouvée dans le Volume Profile !");
            return;
        }

        if (GameCycleManager.Instance.IsDay)
        {
            vignette.intensity.value = 0f;
        }
        else
        {
            vignette.intensity.value = nightIntensity;
        }
    }

    void GoToDay()
    {
        StartFade(0f); // Intensité 0
    }

    void GoToNight()
    {
        StartFade(nightIntensity); // Intensité 0.8 (ou ce que vous avez réglé)
    }

    // Gère le démarrage et l'arrêt de la coroutine
    void StartFade(float targetIntensity)
    {
        if (vignette == null) return;

        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
        }
        currentFadeCoroutine = StartCoroutine(FadeVignette(targetIntensity));
    }

    // La coroutine qui fait le fondu
    IEnumerator FadeVignette(float targetIntensity)
    {
        float startIntensity = vignette.intensity.value;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float normalizedTime = timer / fadeDuration;
            
            // On change la valeur de l'intensité de la vignette
            vignette.intensity.value = Mathf.Lerp(startIntensity, targetIntensity, normalizedTime);
            
            yield return null; 
        }

        vignette.intensity.value = targetIntensity;
        currentFadeCoroutine = null;
    }
}