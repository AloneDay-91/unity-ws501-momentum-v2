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
        audioManager = AudioManager.Instance;

        if (audioManager == null)
        {
            Debug.LogError("SceneAudioController: AudioManager introuvable!");
            return;
        }

        // Arrête toute musique précédente
        StopAllMusic();

        if (playOnStart && !string.IsNullOrEmpty(musicName))
        {
            PlaySceneMusic();
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
        if (audioManager == null || string.IsNullOrEmpty(musicName)) return;

        if (showDebug) Debug.Log($"SceneAudio: Démarrage de '{musicName}'");

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
