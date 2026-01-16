using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// UI pour saisir les pseudos des joueurs dans le menu principal
/// Attache ce script sur un GameObject UI dans le menu
/// </summary>
public class PlayerNameInputUI : MonoBehaviour
{
    [Header("Input Fields")]
    [Tooltip("Champ de saisie pour le joueur 1")]
    public TMP_InputField player1InputField;

    [Tooltip("Champ de saisie pour le joueur 2")]
    public TMP_InputField player2InputField;

    [Header("Buttons")]
    [Tooltip("Bouton pour confirmer et démarrer la partie")]
    public Button startGameButton;

    [Header("Validation")]
    [Tooltip("Texte pour afficher les erreurs de validation")]
    public TMP_Text validationText;

    [Tooltip("Longueur minimale du pseudo")]
    public int minNameLength = 3;

    [Tooltip("Longueur maximale du pseudo")]
    public int maxNameLength = 12;

    [Header("Placeholders")]
    [Tooltip("Placeholder pour joueur 1")]
    public string player1Placeholder = "Pseudo Joueur 1";

    [Tooltip("Placeholder pour joueur 2")]
    public string player2Placeholder = "Pseudo Joueur 2";

    void Start()
    {
        // Configure les placeholders
        if (player1InputField != null)
        {
            TMP_Text placeholder = player1InputField.placeholder as TMP_Text;
            if (placeholder != null)
            {
                placeholder.text = player1Placeholder;
            }

            // Charge le nom sauvegardé
            if (PlayerNameManager.Instance != null)
            {
                player1InputField.text = PlayerNameManager.Instance.GetPlayer1Name();
                if (player1InputField.text == "Player 1")
                {
                    player1InputField.text = "";
                }
            }

            // Ajoute un listener pour validation en temps réel
            player1InputField.onValueChanged.AddListener(OnInputChanged);
        }

        if (player2InputField != null)
        {
            TMP_Text placeholder = player2InputField.placeholder as TMP_Text;
            if (placeholder != null)
            {
                placeholder.text = player2Placeholder;
            }

            // Charge le nom sauvegardé
            if (PlayerNameManager.Instance != null)
            {
                player2InputField.text = PlayerNameManager.Instance.GetPlayer2Name();
                if (player2InputField.text == "Player 2")
                {
                    player2InputField.text = "";
                }
            }

            // Ajoute un listener pour validation en temps réel
            player2InputField.onValueChanged.AddListener(OnInputChanged);
        }

        // Configure le bouton start
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(OnStartGameClicked);
        }

        // Cache le texte de validation au départ
        if (validationText != null)
        {
            validationText.gameObject.SetActive(false);
        }

        // Valide au démarrage
        ValidateInputs();
    }

    /// <summary>
    /// Appelé quand un input change
    /// </summary>
    private void OnInputChanged(string value)
    {
        ValidateInputs();
    }

    /// <summary>
    /// Valide les inputs
    /// </summary>
    private bool ValidateInputs()
    {
        string player1 = player1InputField != null ? player1InputField.text.Trim() : "";
        string player2 = player2InputField != null ? player2InputField.text.Trim() : "";

        // Vérifie que les deux champs sont remplis
        if (string.IsNullOrWhiteSpace(player1))
        {
            ShowValidationError("Le joueur 1 doit entrer un pseudo");
            return false;
        }

        if (string.IsNullOrWhiteSpace(player2))
        {
            ShowValidationError("Le joueur 2 doit entrer un pseudo");
            return false;
        }

        // Vérifie la longueur
        if (player1.Length < minNameLength)
        {
            ShowValidationError($"Le pseudo du joueur 1 doit faire au moins {minNameLength} caractères");
            return false;
        }

        if (player2.Length < minNameLength)
        {
            ShowValidationError($"Le pseudo du joueur 2 doit faire au moins {minNameLength} caractères");
            return false;
        }

        if (player1.Length > maxNameLength)
        {
            ShowValidationError($"Le pseudo du joueur 1 ne doit pas dépasser {maxNameLength} caractères");
            return false;
        }

        if (player2.Length > maxNameLength)
        {
            ShowValidationError($"Le pseudo du joueur 2 ne doit pas dépasser {maxNameLength} caractères");
            return false;
        }

        // Vérifie que les pseudos sont différents
        if (player1.Equals(player2, System.StringComparison.OrdinalIgnoreCase))
        {
            ShowValidationError("Les deux joueurs doivent avoir des pseudos différents");
            return false;
        }

        // Tout est OK
        HideValidationError();
        if (startGameButton != null)
        {
            startGameButton.interactable = true;
        }
        return true;
    }

    /// <summary>
    /// Affiche un message d'erreur
    /// </summary>
    private void ShowValidationError(string message)
    {
        if (validationText != null)
        {
            validationText.text = message;
            validationText.color = Color.red;
            validationText.gameObject.SetActive(true);
        }

        if (startGameButton != null)
        {
            startGameButton.interactable = false;
        }
    }

    /// <summary>
    /// Cache le message d'erreur
    /// </summary>
    private void HideValidationError()
    {
        if (validationText != null)
        {
            validationText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Appelé quand on clique sur "Démarrer"
    /// </summary>
    private void OnStartGameClicked()
    {
        if (!ValidateInputs())
        {
            return;
        }

        // Sauvegarde les noms
        if (PlayerNameManager.Instance != null)
        {
            PlayerNameManager.Instance.SetPlayer1Name(player1InputField.text.Trim());
            PlayerNameManager.Instance.SetPlayer2Name(player2InputField.text.Trim());

            Debug.Log($"PlayerNameInputUI: Pseudos sauvegardés - P1: {player1InputField.text}, P2: {player2InputField.text}");
        }
        else
        {
            Debug.LogError("PlayerNameInputUI: PlayerNameManager non trouvé!");
        }

        // La scène sera chargée par un autre script (MainMenuManager par exemple)
    }

    /// <summary>
    /// Méthode publique pour obtenir les noms (pour les utiliser depuis un autre script)
    /// </summary>
    public bool TryGetPlayerNames(out string player1, out string player2)
    {
        player1 = "";
        player2 = "";

        if (!ValidateInputs())
        {
            return false;
        }

        player1 = player1InputField.text.Trim();
        player2 = player2InputField.text.Trim();
        return true;
    }
}
