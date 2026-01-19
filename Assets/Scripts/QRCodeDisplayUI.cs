using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using TMPro;
using Anatidae;

/// <summary>
/// Affiche les QR codes pour que les joueurs rejoignent la partie
/// Utilise l'API QR Server pour générer les images de QR codes
/// </summary>
public class QRCodeDisplayUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Image pour afficher le QR code du joueur 1")]
    public RawImage player1QRImage;

    [Tooltip("Image pour afficher le QR code du joueur 2")]
    public RawImage player2QRImage;

    [Tooltip("Texte de statut du joueur 1")]
    public TMP_Text player1StatusText;

    [Tooltip("Texte de statut du joueur 2")]
    public TMP_Text player2StatusText;

    [Tooltip("Bouton pour démarrer la partie")]
    public Button startGameButton;

    [Tooltip("Texte du bouton start")]
    public TMP_Text startButtonText;

    [Tooltip("Panel contenant les QR codes")]
    public GameObject qrCodePanel;

    [Header("QR Code Settings")]
    [Tooltip("Taille du QR code en pixels (utilisé uniquement pour l'affichage)")]
    public int qrCodeSize = 300;

    [Header("Status Colors")]
    public Color waitingColor = Color.yellow;
    public Color readyColor = Color.green;

    [Header("Debug")]
    public bool showDebug = true;

    private bool isInitialized = false;

    void OnEnable()
    {
        // S'abonne aux événements (IMPORTANT: avant toute autre chose)
        GameSessionManager.OnSessionCreated += OnSessionCreated;
        GameSessionManager.OnPlayer1Joined += OnPlayer1Joined;
        GameSessionManager.OnPlayer2Joined += OnPlayer2Joined;
        GameSessionManager.OnBothPlayersReady += OnBothPlayersReady;

        // Réinitialise l'affichage quand la page s'active
        if (startGameButton != null)
        {
            startGameButton.interactable = false;
            if (startButtonText != null)
            {
                startButtonText.text = "En attente des joueurs...";
            }
        }

        // Met à jour les textes de statut
        UpdateStatusText(player1StatusText, "En attente...", waitingColor);
        UpdateStatusText(player2StatusText, "En attente...", waitingColor);

        // Vérifie si une session existe déjà (cas où la session est créée avant l'activation de la page)
        if (GameSessionManager.Instance != null &&
            !string.IsNullOrEmpty(GameSessionManager.Instance.sessionId) &&
            !isInitialized)
        {
            if (showDebug)
            {
                Debug.Log("QRCodeDisplayUI: Session déjà existante détectée, chargement des QR codes...");
            }
            OnSessionCreated();
        }
    }

    void OnDisable()
    {
        // Se désabonne des événements
        GameSessionManager.OnSessionCreated -= OnSessionCreated;
        GameSessionManager.OnPlayer1Joined -= OnPlayer1Joined;
        GameSessionManager.OnPlayer2Joined -= OnPlayer2Joined;
        GameSessionManager.OnBothPlayersReady -= OnBothPlayersReady;
    }

    private void OnSessionCreated()
    {
        if (showDebug)
        {
            Debug.Log("QRCodeDisplayUI: Session créée, génération des QR codes...");
        }

        // Charge les QR codes depuis l'API
        if (GameSessionManager.Instance != null)
        {
            StartCoroutine(LoadQRCode(player1QRImage, GameSessionManager.Instance.player1QRCodeUrl));
            StartCoroutine(LoadQRCode(player2QRImage, GameSessionManager.Instance.player2QRCodeUrl));
        }

        isInitialized = true;
    }

    private void OnPlayer1Joined(string pseudo)
    {
        UpdateStatusText(player1StatusText, $"✓ {pseudo}", readyColor);

        if (showDebug)
        {
            Debug.Log($"QRCodeDisplayUI: Joueur 1 prêt - {pseudo}");
        }
    }

    private void OnPlayer2Joined(string pseudo)
    {
        UpdateStatusText(player2StatusText, $"✓ {pseudo}", readyColor);

        if (showDebug)
        {
            Debug.Log($"QRCodeDisplayUI: Joueur 2 prêt - {pseudo}");
        }
    }

    private void OnBothPlayersReady()
    {
        // Active le bouton start
        if (startGameButton != null)
        {
            startGameButton.interactable = true;
            if (startButtonText != null)
            {
                startButtonText.text = "DÉMARRER LA PARTIE";
            }
        }

        if (showDebug)
        {
            Debug.Log("QRCodeDisplayUI: Les deux joueurs sont prêts!");
        }

        // Passe automatiquement à la page lobby
        if (MenuPageManager.Instance != null)
        {
            MenuPageManager.Instance.ShowLobbyPage();
        }
    }

    private IEnumerator LoadQRCode(RawImage targetImage, string qrCodeUrl)
    {
        if (targetImage == null || string.IsNullOrEmpty(qrCodeUrl))
        {
            Debug.LogError("QRCodeDisplayUI: RawImage ou URL du QR code invalide");
            yield break;
        }

        if (showDebug)
        {
            Debug.Log($"QRCodeDisplayUI: Chargement du QR code depuis {qrCodeUrl}");
        }

        // Active le RawImage s'il est désactivé
        if (!targetImage.enabled)
        {
            targetImage.enabled = true;
            if (showDebug)
            {
                Debug.Log("QRCodeDisplayUI: RawImage activé");
            }
        }

        // Utilise UnityWebRequestTexture directement (fonctionne si le serveur a les headers CORS)
        // Le proxy Anatidae corrompt les données binaires, donc on charge directement
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(qrCodeUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                targetImage.texture = texture;

                if (showDebug)
                {
                    Debug.Log($"QRCodeDisplayUI: QR code chargé avec succès ({texture.width}x{texture.height})");
                }
            }
            else
            {
                Debug.LogError($"QRCodeDisplayUI: Erreur de chargement du QR code - {request.error}");
            }
        }
    }

    private void UpdateStatusText(TMP_Text textComponent, string message, Color color)
    {
        if (textComponent != null)
        {
            textComponent.text = message;
            textComponent.color = color;
        }
    }

    /// <summary>
    /// Appelé par le bouton Start pour lancer la partie
    /// </summary>
    public void OnStartGameButtonClicked()
    {
        if (!GameSessionManager.Instance.bothPlayersReady)
        {
            Debug.LogWarning("QRCodeDisplayUI: Les deux joueurs ne sont pas encore prêts");
            return;
        }

        if (showDebug)
        {
            Debug.Log("QRCodeDisplayUI: Démarrage de la partie...");
        }

        // Cache le panel des QR codes
        if (qrCodePanel != null)
        {
            qrCodePanel.SetActive(false);
        }

        // La scène sera chargée par MainMenuManager ou autre
        // Ici on peut juste notifier qu'on est prêt à démarrer
    }

    /// <summary>
    /// Rafraîchit les QR codes (si besoin de recréer une session)
    /// </summary>
    public void RefreshSession()
    {
        if (GameSessionManager.Instance != null)
        {
            GameSessionManager.Instance.ResetSession();
            GameSessionManager.Instance.CreateGameSession();
        }

        // Réinitialise l'UI
        if (startGameButton != null)
        {
            startGameButton.interactable = false;
            if (startButtonText != null)
            {
                startButtonText.text = "En attente des joueurs...";
            }
        }

        UpdateStatusText(player1StatusText, "En attente...", waitingColor);
        UpdateStatusText(player2StatusText, "En attente...", waitingColor);
    }
}
