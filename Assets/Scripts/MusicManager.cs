using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gère la musique de fond du jeu
/// Place ce script sur un GameObject dans ta scène (ex: "MusicManager")
/// </summary>
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Music Settings")]
    [Tooltip("Nom de la musique à jouer (configurée dans AudioManager)")]
    public string musicName = "backgroundMusic";

    [Tooltip("Jouer la musique automatiquement au démarrage")]
    public bool playOnStart = true;

    [Tooltip("Continuer la musique entre les scènes (ne pas redémarrer)")]
    public bool persistBetweenScenes = true;

    [Tooltip("Fade in au démarrage (durée en secondes)")]
    public float fadeInDuration = 2f;

    [Tooltip("Volume cible de la musique")]
    [Range(0f, 1f)]
    public float targetVolume = 0.7f;

    [Header("Scene-Specific Music (optionnel)")]
    [Tooltip("Musiques différentes selon les scènes")]
    public SceneMusic[] sceneMusicList;

    [System.Serializable]
    public class SceneMusic
    {
        public string sceneName;
        public string musicName;
    }

    // État interne
    private AudioManager audioManager;
    private string currentlyPlayingMusic = "";
    private bool isFading = false;
    private float currentVolume = 0f;

    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;

            if (persistBetweenScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
    }

    void Start()
    {
        audioManager = AudioManager.Instance;

        if (audioManager == null)
        {
            Debug.LogWarning("MusicManager: AudioManager introuvable !");
            return;
        }

        if (playOnStart)
        {
            // Vérifie si on doit jouer une musique spécifique à cette scène
            string musicToPlay = GetMusicForCurrentScene();
            PlayMusic(musicToPlay);
        }

        // S'abonne au changement de scène
        if (persistBetweenScenes)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
    }

    void OnDestroy()
    {
        if (persistBetweenScenes)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    void Update()
    {
        // Gère le fade in
        if (isFading && currentVolume < targetVolume)
        {
            currentVolume += Time.deltaTime / fadeInDuration;
            currentVolume = Mathf.Min(currentVolume, targetVolume);

            if (audioManager != null)
            {
                audioManager.SetMusicVolume(currentVolume);
            }

            if (currentVolume >= targetVolume)
            {
                isFading = false;
            }
        }
    }

    /// <summary>
    /// Joue une musique avec fade in
    /// </summary>
    public void PlayMusic(string newMusicName)
    {
        if (audioManager == null || string.IsNullOrEmpty(newMusicName)) return;

        // Si c'est déjà la musique en cours, ne rien faire
        if (currentlyPlayingMusic == newMusicName)
        {
            return;
        }

        // Arrête la musique précédente si elle existe
        if (!string.IsNullOrEmpty(currentlyPlayingMusic))
        {
            audioManager.StopSound(currentlyPlayingMusic);
        }

        // Lance la nouvelle musique
        currentlyPlayingMusic = newMusicName;
        currentVolume = 0f;
        isFading = fadeInDuration > 0f;

        audioManager.SetMusicVolume(isFading ? 0f : targetVolume);
        audioManager.PlayMusic(newMusicName);

        Debug.Log($"MusicManager: Joue maintenant '{newMusicName}'");
    }

    /// <summary>
    /// Arrête la musique avec fade out
    /// </summary>
    public void StopMusic(float fadeOutDuration = 1f)
    {
        if (audioManager == null || string.IsNullOrEmpty(currentlyPlayingMusic)) return;

        if (fadeOutDuration > 0f)
        {
            StartCoroutine(FadeOutCoroutine(fadeOutDuration));
        }
        else
        {
            audioManager.StopSound(currentlyPlayingMusic);
            currentlyPlayingMusic = "";
        }
    }

    /// <summary>
    /// Change le volume de la musique
    /// </summary>
    public void SetVolume(float volume)
    {
        targetVolume = Mathf.Clamp01(volume);
        if (audioManager != null && !isFading)
        {
            audioManager.SetMusicVolume(targetVolume);
        }
    }

    /// <summary>
    /// Met la musique en pause
    /// </summary>
    public void PauseMusic()
    {
        if (audioManager != null && !string.IsNullOrEmpty(currentlyPlayingMusic))
        {
            audioManager.StopSound(currentlyPlayingMusic);
        }
    }

    /// <summary>
    /// Reprend la musique
    /// </summary>
    public void ResumeMusic()
    {
        if (audioManager != null && !string.IsNullOrEmpty(currentlyPlayingMusic))
        {
            audioManager.PlayMusic(currentlyPlayingMusic);
        }
    }

    /// <summary>
    /// Appelé quand une nouvelle scène est chargée
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string musicToPlay = GetMusicForScene(scene.name);

        if (!string.IsNullOrEmpty(musicToPlay) && musicToPlay != currentlyPlayingMusic)
        {
            PlayMusic(musicToPlay);
        }
    }

    /// <summary>
    /// Récupère la musique à jouer pour la scène actuelle
    /// </summary>
    private string GetMusicForCurrentScene()
    {
        return GetMusicForScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Récupère la musique à jouer pour une scène spécifique
    /// </summary>
    private string GetMusicForScene(string sceneName)
    {
        // Cherche dans la liste des musiques par scène
        if (sceneMusicList != null)
        {
            foreach (SceneMusic sceneMusic in sceneMusicList)
            {
                if (sceneMusic.sceneName == sceneName)
                {
                    return sceneMusic.musicName;
                }
            }
        }

        // Sinon, utilise la musique par défaut
        return musicName;
    }

    /// <summary>
    /// Coroutine pour le fade out
    /// </summary>
    private System.Collections.IEnumerator FadeOutCoroutine(float duration)
    {
        float startVolume = currentVolume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float volume = Mathf.Lerp(startVolume, 0f, timer / duration);
            if (audioManager != null)
            {
                audioManager.SetMusicVolume(volume);
            }
            yield return null;
        }

        if (audioManager != null)
        {
            audioManager.StopSound(currentlyPlayingMusic);
            audioManager.SetMusicVolume(targetVolume);
        }

        currentlyPlayingMusic = "";
    }
}
