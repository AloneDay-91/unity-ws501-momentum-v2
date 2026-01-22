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

    [Tooltip("Bouton pour ouvrir l'aide / Comment jouer")] // <--- MODIFIÉ
    public Button howToPlayButton;                         // <--- MODIFIÉ (anciennement quitButton)

    [Header("Debug")]
    public bool showDebug = true;

    void Start()
    {
        // Configure le bouton Play
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayButtonClicked);
        }

        // Configure le bouton Comment Jouer
        if (howToPlayButton != null)
        {
            // On retire les anciens listeners par sécurité et on ajoute le nouveau
            howToPlayButton.onClick.RemoveAllListeners();
            howToPlayButton.onClick.AddListener(OnHowToPlayClicked); // <--- MODIFIÉ
        }
    }

    /// <summary>
    /// Appelé quand le bouton Play est cliqué
    /// </summary>
    private void OnPlayButtonClicked()
    {
        if (showDebug) Debug.Log("HomePageUI: Bouton Play cliqué");

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
    /// Appelé quand le bouton Comment Jouer est cliqué
    /// </summary>
    private void OnHowToPlayClicked() // <--- MODIFIÉ (Logique changée)
    {
        if (showDebug) Debug.Log("HomePageUI: Bouton Comment Jouer cliqué");

        // Au lieu de quitter, on navigue vers la page d'aide
        if (MenuPageManager.Instance != null)
        {
            // Appelle la méthode créée dans l'étape précédente
            MenuPageManager.Instance.ShowHowToPlayPage();
        }
        else
        {
            Debug.LogError("HomePageUI: MenuPageManager non trouvé ! Impossible d'afficher l'aide.");
        }
    }
}
