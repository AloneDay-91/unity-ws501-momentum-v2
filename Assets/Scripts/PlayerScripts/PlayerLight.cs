using UnityEngine;

[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(PlayerInput))] 
public class PlayerLight : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("La lumière enfant (Spot Light) qui sert de 'glow'.")]
    public Light playerGlowLight;

    [Header("Réglages du Glow (Nuit)")]
    public float maxGlowIntensity = 15.0f; // (Vous pouvez garder une valeur élevée)
    public float drainRate = 5.0f; 
    [Tooltip("Définit comment l'intensité de la lumière réagit au remplissage de la barre.")]
    public AnimationCurve intensityCurve;
    
    [Header("Réglages de la Gêne (Jour)")]
    public float stunRange = 3.0f; 
    public float stunCooldown = 5.0f; 
    public LayerMask opponentLayer; 
    
    private float stunCooldownTimer = 0f; 
    private PlayerStats playerStats;
    private PlayerInput playerInput; 
    private bool isLightActive = false; 

    // (Les variables pour la rotation ont été supprimées)

    void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        playerInput = GetComponent<PlayerInput>(); 
        if (playerGlowLight == null)
        {
            Debug.LogError("La lumière 'PlayerGlow' n'est pas assignée !");
        }
        // (L'initialisation des rotations est supprimée)
    }

    // S'abonne à l'événement du jour
    void OnEnable() { GameCycleManager.OnDayStart += TurnOffLight; }
    void OnDisable() { GameCycleManager.OnDayStart -= TurnOffLight; }

    void Start()
    {
        TurnOffLight(); // S'assure que la lumière est éteinte au démarrage
    }
    
    void TurnOnLight()
    {
        if (playerGlowLight != null)
        {
            playerGlowLight.gameObject.SetActive(true); 
            isLightActive = true; 
        }
    }

    void TurnOffLight()
    {
        if (playerGlowLight != null)
        {
            playerGlowLight.gameObject.SetActive(false); 
        }
        isLightActive = false;
    }

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
            if (GameCycleManager.Instance.IsDay) // --- LOGIQUE DE JOUR (GÊNE) ---
            {
                if (stunCooldownTimer <= 0)
                {
                    AttemptStun();
                }
            }
            else // --- LOGIQUE DE NUIT (LUMIÈRE) ---
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

        // (La logique 'HandleLightFlipping' est supprimée)

        // 2. Gérer la consommation
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
    
    // (La fonction HandleLightFlipping est supprimée)
    
    void AttemptStun()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, stunRange, opponentLayer);

        if (hits.Length > 0)
        {
            foreach (Collider hit in hits)
            {
                PlayerInput hitInput = hit.GetComponent<PlayerInput>();
                if (hitInput != null && hitInput.playerID != playerInput.playerID)
                {
                    InterferenceSystem.Instance.AttemptInterference(playerInput.playerID);
                    stunCooldownTimer = stunCooldown;
                    break; 
                }
            }
        }
    }
}