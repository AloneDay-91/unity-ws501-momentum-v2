using UnityEngine;
#if WEB_BUILD
using System;
#endif

public class InterferenceSystem : MonoBehaviour
{
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
    private bool _previousIsStunned = false;
    private Action _removeOnChange;

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
        _removeOnChange?.Invoke();
        _removeOnChange = null;
    }

    private void HandlePlayerAdded(string sessionId, PlayerState state)
    {
        // Listen only to OUR own state — the server toggles isStunned on us when we get hit
        if (NetworkManager.Instance == null || sessionId != NetworkManager.Instance.MySessionId) return;
        if (NetworkManager.Instance.Callbacks == null) return;

        _previousIsStunned = state.isStunned;
        _removeOnChange = NetworkManager.Instance.Callbacks.OnChange(state, () =>
        {
            // Detect false → true transition
            if (state.isStunned && !_previousIsStunned)
            {
                var localMovement = FindLocalPlayerMovement();
                if (localMovement != null) localMovement.ApplyStun(stunDuration);
            }
            _previousIsStunned = state.isStunned;
        });
    }

    private PlayerMovement FindLocalPlayerMovement()
    {
        var local = FindObjectOfType<LocalPlayerSync>();
        return local != null ? local.GetComponent<PlayerMovement>() : null;
    }
#endif

    public void AttemptInterference(int attackerPlayerID)
    {
#if WEB_BUILD
        NetworkManager.Instance?.SendStun();
        return;
#else
        if (attackerPlayerID == 1)
        {
            if (player2 != null) player2.ApplyStun(stunDuration);
        }
        else if (attackerPlayerID == 2)
        {
            if (player1 != null) player1.ApplyStun(stunDuration);
        }
#endif
    }
}
