using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Nom de la scène de jeu à charger (ex: main)")]
    [SerializeField] private string gameSceneName = "main";

    [Header("Input Settings")]
    [Tooltip("Nom du bouton pour lancer le jeu (ex: P1_B1)")]
    [SerializeField] private string playButtonName = "P1_B1";

    [Tooltip("Activer le contrôle par bouton arcade")]
    [SerializeField] private bool enableArcadeInput = true;

    [Header("Audio Settings")]
    [Tooltip("Musique de fond du menu")]
    [SerializeField] private AudioClip menuMusic;

    [Tooltip("Volume de la musique (0-1)")]
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.5f;

    [Tooltip("Pitch de la musique (0.5-2, normal = 1)")]
    [Range(0.5f, 2f)]
    [SerializeField] private float musicPitch = 1f;

    private AudioSource audioSource;

    void Start()
    {
        // Créer et configurer l'AudioSource pour la musique
        audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (menuMusic != null)
        {
            audioSource.clip = menuMusic;
            audioSource.volume = musicVolume;
            audioSource.pitch = musicPitch;
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.Play();
            Debug.Log($"Musique du menu lancée - Volume: {musicVolume}, Pitch: {musicPitch}");
        }
        else
        {
            Debug.LogWarning("Aucune musique assignée au menu !");
        }
    }

    void Update()
    {
        // Détecter l'appui sur le bouton P1_B1 pour lancer le jeu
        if (enableArcadeInput && Input.GetButtonDown(playButtonName))
        {
            PlayGame();
        }
    }

    /// <summary>
    /// Charge la scène de jeu principale
    /// Appelé par le bouton "Jouer" ou par P1_B1
    /// </summary>
    public void PlayGame()
    {
        Debug.Log($"Chargement de la scène : {gameSceneName}");
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// Quitte l'application
    /// Appelé par le bouton "Quitter"
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Fermeture du jeu");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// Ouvre une URL (pour les crédits, site web, etc.)
    /// </summary>
    /// <param name="url">L'URL à ouvrir</param>
    public void OpenURL(string url)
    {
        Application.OpenURL(url);
    }
}
