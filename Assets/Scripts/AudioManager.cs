using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gestionnaire audio centralisé pour tous les sons du jeu
/// Usage: AudioManager.Instance.PlaySound("collect");
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.5f, 1.5f)] public float pitch = 1f;
        [Range(0f, 0.3f)] public float pitchVariation = 0.1f;
        public bool loop = false;

        [HideInInspector] public AudioSource source;
    }

    [Header("Sound Effects")]
    public Sound[] sounds;

    [Header("Settings")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;

    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    [Range(0f, 1f)]
    public float musicVolume = 1f;

    private Dictionary<string, Sound> soundDictionary;

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
            DontDestroyOnLoad(gameObject);
        }

        // Initialisation du dictionnaire
        soundDictionary = new Dictionary<string, Sound>();

        // Création des AudioSources pour chaque son
        foreach (Sound sound in sounds)
        {
            if (sound.clip == null)
            {
                Debug.LogWarning($"AudioClip manquant pour le son: {sound.name}");
                continue;
            }

            sound.source = gameObject.AddComponent<AudioSource>();
            sound.source.clip = sound.clip;
            sound.source.volume = sound.volume;
            sound.source.pitch = sound.pitch;
            sound.source.loop = sound.loop;

            soundDictionary[sound.name] = sound;
        }
    }

    /// <summary>
    /// Joue un son par son nom
    /// </summary>
    public void PlaySound(string soundName)
    {
        if (!soundDictionary.ContainsKey(soundName))
        {
            Debug.LogWarning($"Son non trouvé: {soundName}");
            return;
        }

        Sound sound = soundDictionary[soundName];
        if (sound.source == null) return;

        // Variation de pitch aléatoire
        float randomPitch = sound.pitch + Random.Range(-sound.pitchVariation, sound.pitchVariation);
        sound.source.pitch = randomPitch;

        // Volume avec master
        sound.source.volume = sound.volume * sfxVolume * masterVolume;

        sound.source.Play();
    }

    /// <summary>
    /// Joue un son à une position 3D
    /// </summary>
    public void PlaySoundAtPosition(string soundName, Vector3 position)
    {
        if (!soundDictionary.ContainsKey(soundName))
        {
            Debug.LogWarning($"Son non trouvé: {soundName}");
            return;
        }

        Sound sound = soundDictionary[soundName];
        if (sound.clip == null) return;

        float randomPitch = sound.pitch + Random.Range(-sound.pitchVariation, sound.pitchVariation);
        float volume = sound.volume * sfxVolume * masterVolume;

        AudioSource.PlayClipAtPoint(sound.clip, position, volume);
    }

    /// <summary>
    /// Arrête un son
    /// </summary>
    public void StopSound(string soundName)
    {
        if (!soundDictionary.ContainsKey(soundName))
        {
            Debug.LogWarning($"Son non trouvé: {soundName}");
            return;
        }

        Sound sound = soundDictionary[soundName];
        if (sound.source != null && sound.source.isPlaying)
        {
            sound.source.Stop();
        }
    }

    /// <summary>
    /// Joue une musique en boucle
    /// </summary>
    public void PlayMusic(string musicName)
    {
        if (!soundDictionary.ContainsKey(musicName))
        {
            Debug.LogWarning($"Musique non trouvée: {musicName}");
            return;
        }

        Sound music = soundDictionary[musicName];
        if (music.source == null) return;

        music.source.volume = music.volume * musicVolume * masterVolume;
        music.source.loop = true;
        music.source.Play();
    }

    /// <summary>
    /// Change le volume master
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
    }

    /// <summary>
    /// Change le volume des effets sonores
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
    }

    /// <summary>
    /// Change le volume de la musique
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
    }

    private void UpdateAllVolumes()
    {
        foreach (Sound sound in sounds)
        {
            if (sound.source != null && sound.source.isPlaying)
            {
                float volumeMultiplier = sound.loop ? musicVolume : sfxVolume;
                sound.source.volume = sound.volume * volumeMultiplier * masterVolume;
            }
        }
    }
}
