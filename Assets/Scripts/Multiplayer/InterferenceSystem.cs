using UnityEngine;
#if WEB_BUILD
using System.Collections.Generic;
using Colyseus.Schema;
#endif

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

#if WEB_BUILD
    void Start()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnPlayerAdded += HandlePlayerAdded;
        }
    }

    void OnDestroy()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnPlayerAdded -= HandlePlayerAdded;
        }
    }

    private void HandlePlayerAdded(string sessionId, PlayerState state)
    {
        // Only listen to OUR own state — that's where the server says we got stunned
        if (NetworkManager.Instance == null || sessionId != NetworkManager.Instance.MySessionId) return;

        state.OnChange += (List<DataChange> changes) =>
        {
            foreach (var c in changes)
            {
                if (c.Field == "isStunned" && c.Value is bool isStunned && isStunned)
                {
                    var localMovement = FindLocalPlayerMovement();
                    if (localMovement != null) localMovement.ApplyStun(stunDuration);
                }
            }
        };
    }

    private PlayerMovement FindLocalPlayerMovement()
    {
        var local = FindObjectOfType<LocalPlayerSync>();
        return local != null ? local.GetComponent<PlayerMovement>() : null;
    }
#endif

    // Fonction appelée par PlayerLight.cs
    public void AttemptInterference(int attackerPlayerID)
    {
#if WEB_BUILD
        NetworkManager.Instance?.SendStun();
        return;
#else
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
#endif
    }
}
