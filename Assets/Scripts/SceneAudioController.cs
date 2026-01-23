using UnityEngine;

/// <summary>
/// Contrôleur audio simple pour chaque scène
/// Chaque scène a son propre SceneAudioController qui gère sa musique
/// La musique s'arrête automatiquement quand on quitte la scène
/// </summary>
public class SceneAudioController : MonoBehaviour
{
    [Header("Musique de la scène")]
    [Tooltip("Nom de la musique à jouer (configurée dans AudioManager)")]
    public string musicName = "";

    [Tooltip("Jouer la musique automatiquement au démarrage")]
    public bool playOnStart = true;

    [Tooltip("Durée du fade in (0 = pas de fade)")]
    public float fadeInDuration = 1f;

    [Tooltip("Durée du fade out quand on quitte (0 = arrêt immédiat)")]
    public float fadeOutDuration = 0.5f;

    [Tooltip("Volume cible de la musique")]
    [Range(0f, 1f)]
    public float targetVolume = 0.7f;

    [Header("Debug")]
    public bool showDebug = true;

    // État interne
    private AudioManager audioManager;
    private bool isPlaying = false;
    private bool isFadingIn = false;
    private bool isFadingOut = false;
    private float currentVolume = 0f;

    void Start()
    {
        StartCoroutine(InitializeAudio());
    }

    /// <summary>
    /// Initialise l'audio avec un petit délai pour s'assurer que AudioManager est prêt
    /// </summary>
    private System.Collections.IEnumerator InitializeAudio()
    {
        // Attend une frame pour s'assurer que tous les singletons sont initialisés
        yield return null;

        audioManager = AudioManager.Instance;

        if (audioManager == null)
        {
            Debug.LogError("SceneAudioController: AudioManager introuvable! Assurez-vous qu'il existe dans la scène ou qu'il persiste entre les scènes.");
            yield break;
        }

        if (showDebug)
        {
            Debug.Log($"SceneAudioController: AudioManager trouvé, initialisation de l'audio pour cette scène...");
        }

        // Arrête toute musique précédente
        StopAllMusic();

        if (playOnStart && !string.IsNullOrEmpty(musicName))
        {
            PlaySceneMusic();
        }
        else if (string.IsNullOrEmpty(musicName))
        {
            Debug.LogWarning("SceneAudioController: Aucun nom de musique configuré (musicName est vide)!");
        }
    }

    void Update()
    {
        if (audioManager == null) return;

        // Gère le fade in
        if (isFadingIn && currentVolume < targetVolume)
        {
            currentVolume += Time.deltaTime / fadeInDuration;
            currentVolume = Mathf.Min(currentVolume, targetVolume);
            audioManager.SetMusicVolume(currentVolume);

            if (currentVolume >= targetVolume)
            {
                isFadingIn = false;
                if (showDebug) Debug.Log($"SceneAudio: Fade in terminé pour '{musicName}'");
            }
        }

        // Gère le fade out
        if (isFadingOut && currentVolume > 0f)
        {
            currentVolume -= Time.deltaTime / fadeOutDuration;
            currentVolume = Mathf.Max(currentVolume, 0f);
            audioManager.SetMusicVolume(currentVolume);

            if (currentVolume <= 0f)
            {
                isFadingOut = false;
                audioManager.StopSound(musicName);
                if (showDebug) Debug.Log($"SceneAudio: Fade out terminé pour '{musicName}'");
            }
        }
    }

    void OnDestroy()
    {
        // Arrête la musique quand le script/scène est détruit
        StopSceneMusic(false); // Pas de fade, arrêt immédiat
    }

    /// <summary>
    /// Joue la musique de cette scène
    /// </summary>
    public void PlaySceneMusic()
    {
        if (audioManager == null)
        {
            Debug.LogError("SceneAudio: audioManager est null!");
            return;
        }

        if (string.IsNullOrEmpty(musicName))
        {
            Debug.LogError("SceneAudio: musicName est vide!");
            return;
        }

        if (showDebug) Debug.Log($"SceneAudio: Tentative de démarrage de '{musicName}'...");

        // Vérifie si le son existe dans AudioManager
        bool soundFound = false;
        if (audioManager.sounds != null)
        {
            foreach (var sound in audioManager.sounds)
            {
                if (sound.name == musicName)
                {
                    soundFound = true;
                    if (showDebug)
                    {
                        Debug.Log($"SceneAudio: Son '{musicName}' trouvé! Clip: {(sound.clip != null ? sound.clip.name : "NULL")}, Loop: {sound.loop}");
                    }
                    break;
                }
            }
        }

        if (!soundFound)
        {
            Debug.LogError($"SceneAudio: Le son '{musicName}' n'existe pas dans AudioManager! Vérifiez le nom exact.");
            return;
        }

        isPlaying = true;

        if (fadeInDuration > 0f)
        {
            currentVolume = 0f;
            isFadingIn = true;
            audioManager.SetMusicVolume(0f);
        }
        else
        {
            currentVolume = targetVolume;
            audioManager.SetMusicVolume(targetVolume);
        }

        audioManager.PlayMusic(musicName);

        if (showDebug) Debug.Log($"SceneAudio: PlayMusic('{musicName}') appelé");
    }

    /// <summary>
    /// Arrête la musique de cette scène
    /// </summary>
    public void StopSceneMusic(bool useFade = true)
    {
        if (audioManager == null || string.IsNullOrEmpty(musicName) || !isPlaying) return;

        if (showDebug) Debug.Log($"SceneAudio: Arrêt de '{musicName}'");

        isPlaying = false;
        isFadingIn = false;

        if (useFade && fadeOutDuration > 0f)
        {
            isFadingOut = true;
        }
        else
        {
            audioManager.StopSound(musicName);
            currentVolume = 0f;
        }
    }

    /// <summary>
    /// Arrête toutes les musiques en cours (utile au changement de scène)
    /// </summary>
    private void StopAllMusic()
    {
        if (audioManager == null) return;

        // Parcourt tous les sons et arrête ceux qui sont en loop (musiques)
        if (audioManager.sounds != null)
        {
            foreach (var sound in audioManager.sounds)
            {
                if (sound.loop && sound.source != null && sound.source.isPlaying)
                {
                    sound.source.Stop();
                    if (showDebug) Debug.Log($"SceneAudio: Arrêt de la musique précédente '{sound.name}'");
                }
            }
        }
    }

    /// <summary>
    /// Change le volume de la musique
    /// </summary>
    public void SetVolume(float volume)
    {
        targetVolume = Mathf.Clamp01(volume);
        if (!isFadingIn && !isFadingOut && audioManager != null)
        {
            currentVolume = targetVolume;
            audioManager.SetMusicVolume(targetVolume);
        }
    }

    /// <summary>
    /// Met en pause la musique
    /// </summary>
    public void PauseMusic()
    {
        if (audioManager != null && !string.IsNullOrEmpty(musicName))
        {
            audioManager.StopSound(musicName);
        }
    }

    /// <summary>
    /// Reprend la musique
    /// </summary>
    public void ResumeMusic()
    {
        if (audioManager != null && !string.IsNullOrEmpty(musicName) && isPlaying)
        {
            audioManager.PlayMusic(musicName);
        }
    }
}
