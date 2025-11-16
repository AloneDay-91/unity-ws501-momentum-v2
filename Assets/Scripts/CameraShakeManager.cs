using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gestionnaire de shake pour plusieurs caméras (multi-joueurs)
/// Remplace le singleton CameraShake pour supporter plusieurs caméras
/// Usage: CameraShakeManager.Instance.ShakeAll();
/// </summary>
public class CameraShakeManager : MonoBehaviour
{
    public static CameraShakeManager Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("Trouver automatiquement toutes les caméras au démarrage")]
    public bool autoFindCameras = true;

    [Tooltip("Tag des caméras à secouer (si vide, toutes les caméras)")]
    public string cameraTag = "";

    [Tooltip("Multiplicateur global de l'intensité")]
    [Range(0f, 2f)]
    public float globalShakeMultiplier = 1f;

    private List<CameraShake> cameraShakes = new List<CameraShake>();

    void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    void Start()
    {
        if (autoFindCameras)
        {
            FindAllCameras();
        }
    }

    /// <summary>
    /// Trouve toutes les caméras et leur ajoute CameraShake
    /// </summary>
    public void FindAllCameras()
    {
        cameraShakes.Clear();

        Camera[] cameras;

        // Si un tag est spécifié, chercher par tag
        if (!string.IsNullOrEmpty(cameraTag))
        {
            GameObject[] cameraObjects = GameObject.FindGameObjectsWithTag(cameraTag);
            foreach (GameObject camObj in cameraObjects)
            {
                Camera cam = camObj.GetComponent<Camera>();
                if (cam != null)
                {
                    AddCameraShake(cam);
                }
            }
        }
        else
        {
            // Sinon, toutes les caméras
            cameras = FindObjectsOfType<Camera>();
            foreach (Camera cam in cameras)
            {
                AddCameraShake(cam);
            }
        }

        Debug.Log($"CameraShakeManager: {cameraShakes.Count} caméra(s) trouvée(s)");
    }

    /// <summary>
    /// Ajoute un CameraShake à une caméra
    /// </summary>
    private void AddCameraShake(Camera cam)
    {
        CameraShake shake = cam.GetComponent<CameraShake>();
        if (shake == null)
        {
            shake = cam.gameObject.AddComponent<CameraShake>();
        }

        // Désactive le singleton du CameraShake individuel
        shake.enabled = true;
        cameraShakes.Add(shake);
    }

    /// <summary>
    /// Secoue toutes les caméras
    /// </summary>
    public void ShakeAll(float duration, float intensity)
    {
        foreach (CameraShake shake in cameraShakes)
        {
            if (shake != null)
            {
                shake.Shake(duration, intensity * globalShakeMultiplier);
            }
        }
    }

    /// <summary>
    /// Shake léger sur toutes les caméras
    /// </summary>
    public void ShakeAllLight()
    {
        ShakeAll(0.15f, 0.1f);
    }

    /// <summary>
    /// Shake moyen sur toutes les caméras
    /// </summary>
    public void ShakeAllMedium()
    {
        ShakeAll(0.25f, 0.25f);
    }

    /// <summary>
    /// Shake fort sur toutes les caméras
    /// </summary>
    public void ShakeAllStrong()
    {
        ShakeAll(0.4f, 0.5f);
    }

    /// <summary>
    /// Ajoute manuellement une caméra à secouer
    /// </summary>
    public void RegisterCamera(Camera cam)
    {
        if (cam != null && !cameraShakes.Exists(s => s != null && s.gameObject == cam.gameObject))
        {
            AddCameraShake(cam);
        }
    }
}
