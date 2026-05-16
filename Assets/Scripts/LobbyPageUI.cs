using System.Collections;
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

    [Header("Navigation")]
    [Tooltip("Bouton pour revenir au menu précédent")]
    public Button backButton;

    [Header("Scene Loading")]
    [Tooltip("Nom de la scène de jeu à charger")]
    public string gameSceneName = "main";

    [Header("Debug")]
    public bool showDebug = true;

    void OnEnable()
    {
        // S'abonne à l'événement de démarrage de partie
        GameSessionManager.OnGameStarted += OnGameStarted;

        // S'abonne aux events de pseudo/ready pour rafraîchir l'UI quand
        // les noms arrivent (depuis le polling API en arcade, depuis OnPlayerAdded en WEB_BUILD)
        GameSessionManager.OnPlayer1Joined += OnPlayerInfoChanged;
        GameSessionManager.OnPlayer2Joined += OnPlayerInfoChanged;
        GameSessionManager.OnBothPlayersReady += OnBothPlayersReady;

        // Rafraîchit l'affichage quand la page s'active
        UpdateDisplay();
    }

    void OnDisable()
    {
        // Se désabonne des events
        GameSessionManager.OnGameStarted -= OnGameStarted;
        GameSessionManager.OnPlayer1Joined -= OnPlayerInfoChanged;
        GameSessionManager.OnPlayer2Joined -= OnPlayerInfoChanged;
        GameSessionManager.OnBothPlayersReady -= OnBothPlayersReady;
    }

    private void OnPlayerInfoChanged(string _) => UpdateDisplay();
    private void OnBothPlayersReady() => UpdateDisplay();

    void Start()
    {
        // Configure le bouton
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(OnStartGameClicked);
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackButtonClicked);
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

        // Désactive le bouton pendant le chargement
        if (startGameButton != null) startGameButton.interactable = false;
        if (startButtonText != null) startButtonText.text = "Démarrage...";

#if WEB_BUILD
        // En WEB_BUILD le serveur pilote la transition (state.status → "playing"
        // fait fire OnGameStarted → LoadScene). Mais on lance aussi un fallback
        // manuel après 1.5s au cas où le state listener n'aurait pas fired
        // (ex: désync, listener jamais installé, etc.) — au pire ça LoadScene
        // un peu avant l'autre client, c'est mieux que rester bloqué.
        if (showDebug) Debug.Log("LobbyPageUI: WEB_BUILD - attente serveur (state.status → playing), fallback manuel dans 1.5s");
        StartCoroutine(WebBuildStartFallback());
#else
        if (showDebug) Debug.Log("LobbyPageUI: Démarrage de la partie via l'API...");
        // Appelle l'API pour démarrer la partie (arcade flow)
        GameSessionManager.Instance.StartGame(gameSceneName);
#endif
    }

#if WEB_BUILD
    private bool _webBuildSceneLoadTriggered = false;

    private IEnumerator WebBuildStartFallback()
    {
        yield return new WaitForSeconds(1.5f);
        if (_webBuildSceneLoadTriggered) yield break;
        _webBuildSceneLoadTriggered = true;
        if (showDebug) Debug.Log("LobbyPageUI: WEB_BUILD fallback timer expired — manual LoadScene");
        UnityEngine.SceneManagement.SceneManager.LoadScene(gameSceneName);
    }
#endif

    /// <summary>
    /// Appelé quand l'API confirme que la partie a démarré (arcade) ou que
    /// le serveur Colyseus a flippé state.status sur "playing" (WEB_BUILD).
    /// </summary>
    private void OnGameStarted()
    {
#if WEB_BUILD
        if (_webBuildSceneLoadTriggered) return;
        _webBuildSceneLoadTriggered = true;
#endif
        if (showDebug)
        {
            Debug.Log("LobbyPageUI: Partie démarrée! Chargement de la scène...");
        }

        // Charge la scène de jeu
        UnityEngine.SceneManagement.SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// Retour au menu. En WEB_BUILD la session vient de l'URL et la connexion Colyseus
    /// est persistante — on ne la détruit PAS, on navigue juste vers la home pour que le
    /// joueur puisse consulter « Comment jouer » puis revenir au lobby sans perdre la partie.
    /// </summary>
    public void OnBackButtonClicked()
    {
        if (MenuPageManager.Instance == null) return;

#if WEB_BUILD
        // Pas de ResetSession() : sessionId, room Colyseus, pseudos et bothPlayersReady
        // doivent survivre à un aller-retour dans le menu.
        MenuPageManager.Instance.ShowHomePage();
#else
        // En arcade, le « Retour » annule réellement la session en cours.
        if (GameSessionManager.Instance != null)
        {
            GameSessionManager.Instance.ResetSession();
        }
        MenuPageManager.Instance.ShowQRCodePage();
#endif
    }
}
