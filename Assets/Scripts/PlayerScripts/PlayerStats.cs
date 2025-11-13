using UnityEngine;

[RequireComponent(typeof(PlayerInput))]
public class PlayerStats : MonoBehaviour
{
    [Header("Score")]
    public int vaultScore = 100;
    public int slideScore = 50;
    
    [Header("Luminescence")]
    public float maxLuminescence = 100f;
    public float currentLuminescence { get; private set; }

    private PlayerInput playerInput;

    void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        currentLuminescence = 0; // On commence sans lumière
    }

    // --- SECTION SCORE ---
    public void AddScoreForAction(string actionType)
    {
        int scoreToAdd = 0;

        if (actionType == "Vault")
        {
            scoreToAdd = vaultScore;
        }
        else if (actionType == "Slide")
        {
            scoreToAdd = slideScore;
        }
            
        if (scoreToAdd > 0)
        {
            // Plus tard, on appellera le VRAI ScoreManager
            Debug.Log("Joueur " + playerInput.playerID + " gagne " + scoreToAdd + " points !");
            // ScoreManager.Instance.AddScore(playerInput.playerID, scoreToAdd);
        }
    }

    // --- SECTION LUMINESCENCE ---
    public void AddLuminescence(float amount)
    {
        currentLuminescence += amount;
        if (currentLuminescence > maxLuminescence)
        {
            currentLuminescence = maxLuminescence;
        }
        Debug.Log("Lumière J" + playerInput.playerID + ": " + currentLuminescence);
    }

    public bool UseLuminescence(float amount)
    {
        // Si on n'a pas assez de lumière
        if (currentLuminescence < amount)
        {
            return false; // Échec
        }

        // Si on a assez, on l'utilise
        currentLuminescence -= amount;
        return true; // Succès
    }
    
    // --- NOUVELLE FONCTION ---
    // Fonction pour vider la barre (utilisée par PlayerLight.cs)
    public void DrainLuminescence(float amountToDrain)
    {
        currentLuminescence -= amountToDrain;
        if (currentLuminescence < 0)
        {
            currentLuminescence = 0;
        }
    }
}