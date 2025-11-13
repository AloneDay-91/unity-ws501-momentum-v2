using UnityEngine;

public class InterferenceSystem : MonoBehaviour
{
    // --- Singleton Pattern ---
    public static InterferenceSystem Instance { get; private set; }

    [Header("Références des Joueurs")]
    [Tooltip("Faites glisser l'objet Player_J1 ici.")]
    public PlayerMovement player1;
    [Tooltip("Faites glisser l'objet Player_J2 ici.")]
    public PlayerMovement player2;

    [Header("Réglages de la Gêne")]
    [Tooltip("Durée (en secondes) de l'étourdissement.")]
    public float stunDuration = 1.0f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    // Fonction appelée par PlayerLight.cs
    public void AttemptInterference(int attackerPlayerID)
    {
        // Si le joueur 1 attaque...
        if (attackerPlayerID == 1)
        {
            // ...on étourdit le joueur 2
            if (player2 != null)
            {
                player2.ApplyStun(stunDuration);
            }
        }
        // Si le joueur 2 attaque...
        else if (attackerPlayerID == 2)
        {
            // ...on étourdit le joueur 1
            if (player1 != null)
            {
                player1.ApplyStun(stunDuration);
            }
        }
    }
}