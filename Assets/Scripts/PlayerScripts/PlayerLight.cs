using UnityEngine;

[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(PlayerInput))] 
public class PlayerLight : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("La lumière enfant (Point Light) qui sert de 'glow'.")]
    public Light playerGlowLight;

    [Header("Réglages du Glow (Nuit)")]
    public float maxGlowIntensity = 2.0f;
    public float drainRate = 5.0f; 
    [Tooltip("Définit comment l'intensité de la lumière réagit au remplissage de la barre.")]
    public AnimationCurve intensityCurve;
    
    // --- NOUVELLES VARIABLES ---
    [Header("Réglages de la Gêne (Jour)")]
    [Tooltip("La portée de l'attaque de Gêne. Réglez-la comme votre 'Vault Check Distance' !")]
    public float stunRange = 3.0f; // Distance de détection
    [Tooltip("La durée (en secondes) avant de pouvoir réutiliser la Gêne.")]
    public float stunCooldown = 5.0f; // 5 secondes de cooldown
    [Tooltip("Cochez le Layer 'Player' que vous avez créé.")]
    public LayerMask opponentLayer; // Le calque pour trouver l'adversaire
    
    private float stunCooldownTimer = 0f; // Le chronomètre
    // --- FIN ---

    private PlayerStats playerStats;
    private PlayerInput playerInput; 
    private bool isLightActive = false; 

    void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        playerInput = GetComponent<PlayerInput>(); 
        if (playerGlowLight == null)
        {
            Debug.LogError("La lumière 'PlayerGlow' n'est pas assignée !");
        }
    }

    // ... (OnEnable, OnDisable, Start ne changent pas) ...
    void OnEnable() { GameCycleManager.OnDayStart += TurnOffLight; }
    void OnDisable() { GameCycleManager.OnDayStart -= TurnOffLight; }
    void Start() { TurnOffLight(); }

    void TurnOnLight()
    {
        if (playerGlowLight != null) { playerGlowLight.gameObject.SetActive(true); isLightActive = true; }
    }

    void TurnOffLight()
    {
        if (playerGlowLight != null) { playerGlowLight.gameObject.SetActive(false); }
        isLightActive = false;
    }

    // --- CETTE FONCTION EST MODIFIÉE ---
    void Update()
    {
        // On fait descendre le minuteur du cooldown
        if (stunCooldownTimer > 0)
        {
            stunCooldownTimer -= Time.deltaTime;
        }

        // 1. Gérer l'activation (Toggle)
        if (playerInput.LightTogglePressed)
        {
            // --- LOGIQUE DE JOUR (GÊNE) ---
            if (GameCycleManager.Instance.IsDay)
            {
                // On vérifie si le cooldown est terminé
                if (stunCooldownTimer <= 0)
                {
                    // On tente la Gêne (avec la nouvelle vérification de distance)
                    AttemptStun();
                }
            }
            // --- LOGIQUE DE NUIT (LUMIÈRE) ---
            else
            {
                if (!isLightActive)
                {
                    if (playerStats.currentLuminescence > 0) { TurnOnLight(); }
                }
                else 
                {
                    TurnOffLight();
                }
            }
        }

        // 2. Gérer la consommation (Lumière de nuit)
        if (isLightActive)
        {
            playerStats.DrainLuminescence(drainRate * Time.deltaTime);

            float intensityPercent = playerStats.currentLuminescence / playerStats.maxLuminescence;
            float curveValue = intensityCurve.Evaluate(intensityPercent);
            playerGlowLight.intensity = curveValue * maxGlowIntensity;

            if (playerStats.currentLuminescence <= 0)
            {
                TurnOffLight();
            }
        }
    }
    
    // --- NOUVELLE FONCTION ---
    void AttemptStun()
    {
        // On crée une "bulle" de détection autour de ce joueur
        Collider[] hits = Physics.OverlapSphere(transform.position, stunRange, opponentLayer);

        // On vérifie si on a touché quelque chose
        if (hits.Length > 0)
        {
            // On vérifie que ce n'est pas NOUS-MÊME
            foreach (Collider hit in hits)
            {
                // Si l'objet touché a un PlayerInput...
                PlayerInput hitInput = hit.GetComponent<PlayerInput>();
                if (hitInput != null && hitInput.playerID != playerInput.playerID)
                {
                    // C'est l'adversaire !
                    Debug.Log("Joueur " + playerInput.playerID + " a GÊNÉ Joueur " + hitInput.playerID);

                    // 1. On appelle le système de Gêne
                    InterferenceSystem.Instance.AttemptInterference(playerInput.playerID);

                    // 2. On lance le Cooldown
                    stunCooldownTimer = stunCooldown;

                    // 3. On a trouvé notre cible, on sort de la boucle
                    break; 
                }
            }
        }
    }
}