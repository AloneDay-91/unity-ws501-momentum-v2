using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class SkyColorController : MonoBehaviour
{
    [Header("Exposition du Ciel")]
    [Tooltip("Exposition de la Skybox le jour (1.0 = normal)")]
    public float dayExposure = 1.0f;
    [Tooltip("Exposition de la Skybox la nuit (0.2 = sombre)")]
    public float nightExposure = 0.2f;

    [Header("Configuration du Fondu")]
    [Tooltip("Durée (en secondes) du fondu entre le jour et la nuit.")]
    public float fadeDuration = 2.0f;

    private Camera cam;
    private Coroutine currentFadeCoroutine;
    private Material skyboxMat;

    void Awake()
    {
        cam = GetComponent<Camera>();
        
        // Force le mode Skybox si une Skybox est définie
        if (RenderSettings.skybox != null)
        {
            cam.clearFlags = CameraClearFlags.Skybox;
            skyboxMat = RenderSettings.skybox;
        }
        else
        {
            Debug.LogWarning("SkyColorController: Aucune Skybox assignée dans Lighting Settings !");
        }
    }

    void OnEnable()
    {
        GameCycleManager.OnDayStart += SetDaySky;
        GameCycleManager.OnNightStart += SetNightSky;
    }

    void OnDisable()
    {
        GameCycleManager.OnDayStart -= SetDaySky;
        GameCycleManager.OnNightStart -= SetNightSky;
    }

    void Start()
    {
        // État initial instantané
        if (skyboxMat != null)
        {
            float targetExp = GameCycleManager.Instance.IsDay ? dayExposure : nightExposure;
            skyboxMat.SetFloat("_Exposure", targetExp);
        }
    }

    void SetDaySky()
    {
        StartFade(dayExposure);
    }

    void SetNightSky()
    {
        StartFade(nightExposure);
    }

    void StartFade(float targetExposure)
    {
        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
        }
        
        if (skyboxMat != null)
        {
            currentFadeCoroutine = StartCoroutine(FadeSkybox(targetExposure));
        }
    }

    IEnumerator FadeSkybox(float targetExposure)
    {
        float startExposure = skyboxMat.GetFloat("_Exposure");
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float normalizedTime = timer / fadeDuration;
            
            // Interpolation de l'exposition
            float currentExp = Mathf.Lerp(startExposure, targetExposure, normalizedTime);
            skyboxMat.SetFloat("_Exposure", currentExp);
            
            yield return null; 
        }

        skyboxMat.SetFloat("_Exposure", targetExposure);
        currentFadeCoroutine = null;
    }
    
    // Remet l'exposition normale en quittant pour ne pas casser l'éditeur
    void OnDestroy()
    {
        if (skyboxMat != null)
        {
            skyboxMat.SetFloat("_Exposure", dayExposure);
        }
    }
}