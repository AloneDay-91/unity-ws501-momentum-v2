using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gère les interactions de la page d'accueil
/// </summary>
public class HomePageUI : MonoBehaviour
{
    [Header("Buttons")]
    [Tooltip("Bouton Play pour lancer une partie")]
    public Button playButton;

    [Tooltip("Bouton Quit (optionnel)")]
    public Button quitButton;

    [Header("Debug")]
    public bool showDebug = true;

    void Start()
    {
        // Configure le bouton Play
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayButtonClicked);
        }

        // Configure le bouton Quit
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitButtonClicked);
        }
    }

    /// <summary>
    /// Appelé quand le bouton Play est cliqué
    /// </summary>
    private void OnPlayButtonClicked()
    {
        if (showDebug)
        {
            Debug.Log("HomePageUI: Bouton Play cliqué");
        }

        // Crée une nouvelle session de jeu
        if (GameSessionManager.Instance != null)
        {
            GameSessionManager.Instance.CreateGameSession();
        }
        else
        {
            Debug.LogError("HomePageUI: GameSessionManager non trouvé!");
            return;
        }

        // Affiche la page des QR codes
        if (MenuPageManager.Instance != null)
        {
            MenuPageManager.Instance.ShowQRCodePage();
        }
        else
        {
            Debug.LogError("HomePageUI: MenuPageManager non trouvé!");
        }
    }

    /// <summary>
    /// Appelé quand le bouton Quit est cliqué
    /// </summary>
    private void OnQuitButtonClicked()
    {
        if (showDebug)
        {
            Debug.Log("HomePageUI: Bouton Quit cliqué");
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
