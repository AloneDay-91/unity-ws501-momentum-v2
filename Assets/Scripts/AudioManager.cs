using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Gestionnaire audio centralisé pour tous les sons du jeu
/// Usage: AudioManager.Instance.PlaySound("collect");
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("AudioListener Management")]
    [Tooltip("Gérer automatiquement les AudioListeners (désactive les doublons)")]
    public bool manageAudioListeners = true;

    [Tooltip("Afficher les logs de debug pour AudioListener")]
    public bool showAudioListenerDebug = true;

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
            // Transfère les sons de cette nouvelle instance vers l'instance existante
            Instance.RegisterSounds(this.sounds);
            
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

            CreateAudioSource(sound);
            soundDictionary[sound.name] = sound;
        }

        // S'abonne au changement de scène pour gérer les AudioListeners
        if (manageAudioListeners)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
    }

    /// <summary>
    /// Crée l'AudioSource pour un son donné
    /// </summary>
    private void CreateAudioSource(Sound sound)
    {
        sound.source = gameObject.AddComponent<AudioSource>();
        sound.source.clip = sound.clip;
        sound.source.volume = sound.volume;
        sound.source.pitch = sound.pitch;
        sound.source.loop = sound.loop;
    }

    /// <summary>
    /// Ajoute de nouveaux sons à l'instance existante
    /// </summary>
    public void RegisterSounds(Sound[] newSounds)
    {
        List<Sound> soundsToAdd = new List<Sound>();

        foreach (Sound newSound in newSounds)
        {
            // Vérifie si le son existe déjà
            if (soundDictionary.ContainsKey(newSound.name))
            {
                continue;
            }

            // Prépare le nouveau son
            if (newSound.clip != null)
            {
                CreateAudioSource(newSound);
                soundDictionary[newSound.name] = newSound;
                soundsToAdd.Add(newSound);
                Debug.Log($"AudioManager: Nouveau son enregistré: {newSound.name}");
            }
        }

        // Met à jour le tableau public pour que SceneAudioController puisse le voir
        if (soundsToAdd.Count > 0)
        {
            List<Sound> allSounds = new List<Sound>(sounds);
            allSounds.AddRange(soundsToAdd);
            sounds = allSounds.ToArray();
        }
    }

    void Start()
    {
        // Gère les AudioListeners de la scène initiale (OnSceneLoaded ne se déclenche pas pour la première scène)
        if (manageAudioListeners)
        {
            if (showAudioListenerDebug)
            {
                Debug.Log($"AudioManager: Initialisation de la scène initiale '{SceneManager.GetActiveScene().name}'");
            }
            Invoke(nameof(ManageAudioListeners), 0.1f);
        }
    }

    void OnDestroy()
    {
        if (manageAudioListeners)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    /// <summary>
    /// Appelé quand une nouvelle scène est chargée
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (showAudioListenerDebug)
        {
            Debug.Log($"AudioManager: Scène '{scene.name}' chargée, vérification audio...");
        }

        if (manageAudioListeners)
        {
            // Petit délai pour que tous les objets soient initialisés
            Invoke(nameof(ManageAudioListeners), 0.1f);
        }
    }

    /// <summary>
    /// Gère les AudioListeners pour n'en garder qu'un seul actif
    /// Priorise les listeners sur des GameObjects actifs
    /// </summary>
    public void ManageAudioListeners()
    {
        // Cherche TOUS les AudioListeners (y compris désactivés)
        AudioListener[] allListeners = FindObjectsOfType<AudioListener>(true);

        if (showAudioListenerDebug)
        {
            Debug.Log($"AudioManager: {allListeners.Length} AudioListener(s) trouvé(s) dans la scène");
        }

        AudioListener audioManagerListener = null;
        AudioListener targetListener = null;
        List<AudioListener> candidates = new List<AudioListener>();

        // Trie les listeners
        foreach (AudioListener listener in allListeners)
        {
            // Identifie celui de l'AudioManager
            if (listener.gameObject == gameObject)
            {
                audioManagerListener = listener;
                continue;
            }

            // Garde uniquement les listeners sur des objets ACTIFS dans la hiérarchie
            if (listener.gameObject.activeInHierarchy)
            {
                candidates.Add(listener);
            }
        }

        // CHOIX DU LISTENER ACTIF
        if (candidates.Count > 0)
        {
            // Prend le premier candidat valide (ex: Camera du J1 ou J2 restant)
            targetListener = candidates[0];
        }
        else
        {
            // Aucun listener valide dans la scène -> Fallback sur AudioManager
            if (audioManagerListener == null)
            {
                Debug.LogWarning("AudioManager: Création d'un AudioListener de secours.");
                audioManagerListener = gameObject.AddComponent<AudioListener>();
            }
            targetListener = audioManagerListener;
        }

        // APPLICATION : Active la cible, désactive les autres
        foreach (AudioListener listener in allListeners)
        {
            if (listener == targetListener)
            {
                if (!listener.enabled)
                {
                    listener.enabled = true;
                    if (showAudioListenerDebug)
                    {
                        Debug.Log($"AudioManager: Activation AudioListener sur '{listener.gameObject.name}'");
                    }
                }
            }
            else
            {
                if (listener.enabled)
                {
                    listener.enabled = false;
                    if (showAudioListenerDebug)
                    {
                        Debug.Log($"AudioManager: Désactivation AudioListener sur '{listener.gameObject.name}'");
                    }
                }
            }
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
        if (music.source == null)
        {
            Debug.LogError($"AudioManager: AudioSource null pour '{musicName}'! Recréation...");
            // Tente de recréer l'AudioSource
            music.source = gameObject.AddComponent<AudioSource>();
            music.source.clip = music.clip;
            music.source.loop = true;
        }

        music.source.volume = music.volume * musicVolume * masterVolume;
        music.source.loop = true;
        music.source.Play();

        if (showAudioListenerDebug)
        {
            Debug.Log($"AudioManager: PlayMusic('{musicName}') - Volume: {music.source.volume}, IsPlaying: {music.source.isPlaying}");
        }
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
