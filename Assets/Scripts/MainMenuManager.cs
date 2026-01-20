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

    void Start()
    {
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
