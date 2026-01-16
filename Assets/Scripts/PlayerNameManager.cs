using UnityEngine;

/// <summary>
/// Gère les pseudos des joueurs entrés dans le menu
/// Singleton qui persiste entre les scènes
/// </summary>
public class PlayerNameManager : MonoBehaviour
{
    public static PlayerNameManager Instance { get; private set; }

    // Pseudos des joueurs
    private string player1Name = "";
    private string player2Name = "";

    // Clés pour PlayerPrefs
    private const string PLAYER1_NAME_KEY = "Player1Name";
    private const string PLAYER2_NAME_KEY = "Player2Name";

    void Awake()
    {
        // Singleton pattern avec persistence
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Charge les noms sauvegardés
        LoadPlayerNames();
    }

    /// <summary>
    /// Charge les noms depuis PlayerPrefs
    /// </summary>
    private void LoadPlayerNames()
    {
        player1Name = PlayerPrefs.GetString(PLAYER1_NAME_KEY, "Player 1");
        player2Name = PlayerPrefs.GetString(PLAYER2_NAME_KEY, "Player 2");

        Debug.Log($"PlayerNameManager: Noms chargés - P1: {player1Name}, P2: {player2Name}");
    }

    /// <summary>
    /// Définit le nom du joueur 1
    /// </summary>
    public void SetPlayer1Name(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            name = "Player 1";
        }

        player1Name = name.Trim();
        PlayerPrefs.SetString(PLAYER1_NAME_KEY, player1Name);
        PlayerPrefs.Save();

        Debug.Log($"PlayerNameManager: Joueur 1 → {player1Name}");
    }

    /// <summary>
    /// Définit le nom du joueur 2
    /// </summary>
    public void SetPlayer2Name(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            name = "Player 2";
        }

        player2Name = name.Trim();
        PlayerPrefs.SetString(PLAYER2_NAME_KEY, player2Name);
        PlayerPrefs.Save();

        Debug.Log($"PlayerNameManager: Joueur 2 → {player2Name}");
    }

    /// <summary>
    /// Obtient le nom du joueur par ID
    /// </summary>
    public string GetPlayerName(int playerID)
    {
        switch (playerID)
        {
            case 1:
                return string.IsNullOrEmpty(player1Name) ? "Player 1" : player1Name;
            case 2:
                return string.IsNullOrEmpty(player2Name) ? "Player 2" : player2Name;
            default:
                return $"Player {playerID}";
        }
    }

    /// <summary>
    /// Obtient le nom du joueur 1
    /// </summary>
    public string GetPlayer1Name()
    {
        return string.IsNullOrEmpty(player1Name) ? "Player 1" : player1Name;
    }

    /// <summary>
    /// Obtient le nom du joueur 2
    /// </summary>
    public string GetPlayer2Name()
    {
        return string.IsNullOrEmpty(player2Name) ? "Player 2" : player2Name;
    }

    /// <summary>
    /// Vérifie si les deux joueurs ont entré leur nom
    /// </summary>
    public bool HasBothPlayerNames()
    {
        return !string.IsNullOrWhiteSpace(player1Name) &&
               !string.IsNullOrWhiteSpace(player2Name) &&
               player1Name != "Player 1" &&
               player2Name != "Player 2";
    }

    /// <summary>
    /// Réinitialise les noms aux valeurs par défaut
    /// </summary>
    public void ResetNames()
    {
        player1Name = "Player 1";
        player2Name = "Player 2";
        PlayerPrefs.DeleteKey(PLAYER1_NAME_KEY);
        PlayerPrefs.DeleteKey(PLAYER2_NAME_KEY);
        PlayerPrefs.Save();

        Debug.Log("PlayerNameManager: Noms réinitialisés");
    }
}
