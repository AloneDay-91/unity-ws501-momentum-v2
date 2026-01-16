using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gère l'affichage de la page Lobby (quand les 2 joueurs sont prêts)
/// </summary>
public class LobbyPageUI : MonoBehaviour
{
    [Header("Player Info")]
    [Tooltip("Texte affichant le pseudo du joueur 1")]
    public TMP_Text player1NameText;

    [Tooltip("Texte affichant le pseudo du joueur 2")]
    public TMP_Text player2NameText;

    [Header("Ready Indicators")]
    [Tooltip("Image/Icon pour indiquer que le joueur 1 est prêt")]
    public GameObject player1ReadyIcon;

    [Tooltip("Image/Icon pour indiquer que le joueur 2 est prêt")]
    public GameObject player2ReadyIcon;

    [Header("Start Button")]
    [Tooltip("Bouton pour démarrer la partie")]
    public Button startGameButton;

    [Tooltip("Texte du bouton start")]
    public TMP_Text startButtonText;

    [Header("Scene Loading")]
    [Tooltip("Nom de la scène de jeu à charger")]
    public string gameSceneName = "main";

    [Header("Debug")]
    public bool showDebug = true;

    void OnEnable()
    {
        // Rafraîchit l'affichage quand la page s'active
        UpdateDisplay();
    }

    void Start()
    {
        // Configure le bouton
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(OnStartGameClicked);
        }

        UpdateDisplay();
    }

    /// <summary>
    /// Met à jour l'affichage avec les infos des joueurs
    /// </summary>
    private void UpdateDisplay()
    {
        if (GameSessionManager.Instance == null)
        {
            Debug.LogWarning("LobbyPageUI: GameSessionManager non trouvé");
            return;
        }

        // Joueur 1
        if (player1NameText != null)
        {
            string player1Name = GameSessionManager.Instance.player1Pseudo;
            if (string.IsNullOrEmpty(player1Name))
            {
                player1Name = "En attente...";
            }
            player1NameText.text = player1Name;
        }

        if (player1ReadyIcon != null)
        {
            player1ReadyIcon.SetActive(GameSessionManager.Instance.player1Ready);
        }

        // Joueur 2
        if (player2NameText != null)
        {
            string player2Name = GameSessionManager.Instance.player2Pseudo;
            if (string.IsNullOrEmpty(player2Name))
            {
                player2Name = "En attente...";
            }
            player2NameText.text = player2Name;
        }

        if (player2ReadyIcon != null)
        {
            player2ReadyIcon.SetActive(GameSessionManager.Instance.player2Ready);
        }

        // Bouton start
        if (startGameButton != null)
        {
            bool canStart = GameSessionManager.Instance.bothPlayersReady;
            startGameButton.interactable = canStart;

            if (startButtonText != null)
            {
                if (canStart)
                {
                    startButtonText.text = "DÉMARRER LA PARTIE";
                }
                else
                {
                    startButtonText.text = "En attente des joueurs...";
                }
            }
        }

        if (showDebug)
        {
            Debug.Log($"LobbyPageUI: Affichage mis à jour - P1: {GameSessionManager.Instance.player1Pseudo}, P2: {GameSessionManager.Instance.player2Pseudo}");
        }
    }

    /// <summary>
    /// Appelé quand le bouton Start est cliqué
    /// </summary>
    private void OnStartGameClicked()
    {
        if (!GameSessionManager.Instance.bothPlayersReady)
        {
            Debug.LogWarning("LobbyPageUI: Les deux joueurs ne sont pas encore prêts");
            return;
        }

        if (showDebug)
        {
            Debug.Log("LobbyPageUI: Démarrage de la partie...");
        }

        // Charge la scène de jeu
        UnityEngine.SceneManagement.SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// Retour au menu principal (annule la session)
    /// </summary>
    public void OnBackButtonClicked()
    {
        // Réinitialise la session
        if (GameSessionManager.Instance != null)
        {
            GameSessionManager.Instance.ResetSession();
        }

        // Retourne à la page d'accueil
        if (MenuPageManager.Instance != null)
        {
            MenuPageManager.Instance.ShowHomePage();
        }
    }
}
