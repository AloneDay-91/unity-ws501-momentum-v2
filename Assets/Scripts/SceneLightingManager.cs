using UnityEngine;
using System.Collections; 
using UnityEngine.Rendering; // Important pour l'éclairage d'ambiance

public class SceneLightingManager : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Faites glisser votre 'Directional Light' (Soleil) ici.")]
    public Light sunLight; 

    [Header("Réglages d'Intensité (Soleil)")]
    public float dayIntensity = 1.0f; 
    public float nightIntensity = 0.0f; // 0 = Soleil éteint
    
    [Header("Réglages d'Ambiance")]
    public Color dayAmbientColor = new Color(0.2f, 0.2f, 0.2f); // Une ambiance grise
    public Color nightAmbientColor = Color.black; // Noir absolu
    
    [Header("Configuration du Fondu")]
    [Tooltip("Doit être la même durée que le 'SkyColorController'.")]
    public float fadeDuration = 2.0f;

    private Coroutine currentFadeCoroutine;

    // On s'abonne aux événements
    void OnEnable()
    {
        GameCycleManager.OnDayStart += GoToDay;
        GameCycleManager.OnNightStart += GoToNight;
    }

    // On se désabonne
    void OnDisable()
    {
        GameCycleManager.OnDayStart -= GoToDay;
        GameCycleManager.OnNightStart -= GoToNight;
    }

    // Gère l'état au démarrage
    void Start()
    {
        if (GameCycleManager.Instance.IsDay)
        {
            if (sunLight != null) sunLight.intensity = dayIntensity;
            // On force la lumière d'ambiance sur "Couleur"
            RenderSettings.ambientMode = AmbientMode.Flat; 
            RenderSettings.ambientLight = dayAmbientColor;
            RenderSettings.fog = true; // Active le brouillard le jour
        }
        else
        {
            if (sunLight != null) sunLight.intensity = nightIntensity;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = nightAmbientColor;
            RenderSettings.fog = false; // Désactive le brouillard la nuit
        }
    }

    void GoToDay()
    {
        // On active le brouillard immédiatement
        RenderSettings.fog = true; 
        StartFade(dayIntensity, dayAmbientColor);
    }

    void GoToNight()
    {
        // On désactive le brouillard immédiatement
        RenderSettings.fog = false; 
        StartFade(nightIntensity, nightAmbientColor);
    }

    // Gère le démarrage et l'arrêt de la coroutine
    void StartFade(float targetSunIntensity, Color targetAmbientColor)
    {
        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
        }
        currentFadeCoroutine = StartCoroutine(FadeLighting(targetSunIntensity, targetAmbientColor));
    }

    // La coroutine qui fait le fondu
    IEnumerator FadeLighting(float targetSunIntensity, Color targetAmbientColor)
    {
        float startSunIntensity = (sunLight != null) ? sunLight.intensity : 0;
        Color startAmbientColor = RenderSettings.ambientLight;
        float timer = 0f;

        // On s'assure que le mode d'ambiance est sur "Couleur"
        RenderSettings.ambientMode = AmbientMode.Flat;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float normalizedTime = timer / fadeDuration;
            
            // Fait le fondu de la lumière principale
            if (sunLight != null)
            {
                sunLight.intensity = Mathf.Lerp(startSunIntensity, targetSunIntensity, normalizedTime);
            }
            
            // Fait le fondu de la lumière d'ambiance (LA CLÉ)
            RenderSettings.ambientLight = Color.Lerp(startAmbientColor, targetAmbientColor, normalizedTime);
            
            yield return null; 
        }

        // S'assure que les valeurs finales sont exactes
        if (sunLight != null) sunLight.intensity = targetSunIntensity;
        RenderSettings.ambientLight = targetAmbientColor;
        currentFadeCoroutine = null;
    }
}