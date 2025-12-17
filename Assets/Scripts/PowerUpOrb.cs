using UnityEngine;

/// <summary>
/// Orbe de power-up ramassable
/// Applique un effet au joueur qui le ramasse ou à l'adversaire selon le type
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(Collider))]
public class PowerUpOrb : MonoBehaviour
{
    [Header("Power-Up Settings")]
    [Tooltip("Type de power-up (SpeedBoost ou SpeedSlow)")]
    public PowerUpType powerUpType = PowerUpType.SpeedBoost;

    [Header("Particules")]
    [Tooltip("Système de particules permanent autour de l'orbe")]
    public ParticleSystem orbParticles;

    [Tooltip("Objet Sphere à désactiver lors de la collecte")]
    public GameObject sphereObject;

    [Header("Juice Effects")]
    [Tooltip("Activer le camera shake lors de la collecte")]
    public bool enableCameraShake = true;

    [Tooltip("Activer le texte flottant")]
    public bool enableFloatingText = true;

    [Tooltip("Nom du son de collecte (dans AudioManager)")]
    public string collectSoundName = "powerup";

    [Tooltip("Activer le son de collecte")]
    public bool enableCollectSound = true;

    [Tooltip("Effet de particules à jouer lors de la collecte")]
    public GameObject collectEffect;

    // Références
    private MeshRenderer meshRenderer;
    private Collider orbCollider;
    private ObjectPulse objectPulse;

    void Awake()
    {
        // On récupère les composants
        meshRenderer = GetComponent<MeshRenderer>();
        orbCollider = GetComponent<Collider>();
        objectPulse = GetComponent<ObjectPulse>();

        // Si pas assigné, on cherche dans les enfants
        if (orbParticles == null)
        {
            orbParticles = GetComponentInChildren<ParticleSystem>();
        }

        // Cherche la Sphere si pas assignée
        if (sphereObject == null)
        {
            Transform sphereTransform = transform.Find("OrbParticles/Sphere");
            if (sphereTransform != null)
            {
                sphereObject = sphereTransform.gameObject;
            }
        }

        // Ajoute ObjectPulse si pas déjà présent
        if (objectPulse == null)
        {
            objectPulse = gameObject.AddComponent<ObjectPulse>();
        }
    }

    void OnEnable()
    {
        // S'abonne aux événements du cycle jour/nuit
        GameCycleManager.OnDayStart += ShowOrb;
        GameCycleManager.OnNightStart += HideOrb;
    }

    void OnDisable()
    {
        // Se désabonne
        GameCycleManager.OnDayStart -= ShowOrb;
        GameCycleManager.OnNightStart -= HideOrb;
    }

    void Start()
    {
        // Vérifie l'état au démarrage
        if (GameCycleManager.Instance != null && GameCycleManager.Instance.IsDay)
        {
            ShowOrb();
        }
        else
        {
            HideOrb();
        }
    }

    void ShowOrb()
    {
        meshRenderer.enabled = true;
        orbCollider.enabled = true;

        if (orbParticles != null)
        {
            orbParticles.Play();
        }

        if (sphereObject != null)
        {
            sphereObject.SetActive(true);
        }
    }

    void HideOrb()
    {
        meshRenderer.enabled = false;
        orbCollider.enabled = false;

        if (orbParticles != null)
        {
            orbParticles.Stop();
        }

        if (sphereObject != null)
        {
            sphereObject.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Vérifie si c'est un joueur
        PlayerInput playerInput = other.GetComponent<PlayerInput>();
        if (playerInput == null) return;

        PlayerPowerUpManager powerUpManager = other.GetComponent<PlayerPowerUpManager>();
        if (powerUpManager == null)
        {
            Debug.LogWarning($"Le joueur {playerInput.playerID} n'a pas de PlayerPowerUpManager !");
            return;
        }

        // Applique l'effet selon le type de power-up
        switch (powerUpType)
        {
            case PowerUpType.SpeedBoost:
                // Boost pour le joueur qui ramasse
                ApplySpeedBoost(playerInput, powerUpManager);
                break;

            case PowerUpType.SpeedSlow:
                // Ralentissement pour l'AUTRE joueur
                ApplySpeedSlowToOtherPlayer(playerInput);
                break;
        }

        // Effets de collecte
        PlayCollectionEffects();

        // Cache l'orbe
        HideOrb();
    }

    private void ApplySpeedBoost(PlayerInput collector, PlayerPowerUpManager powerUpManager)
    {
        // Applique le boost au joueur qui ramasse
        powerUpManager.ApplySpeedBoost();

        // Texte flottant
        if (enableFloatingText)
        {
            Vector3 textPosition = transform.position + Vector3.up * 0.5f;
            FloatingText.Create("SPEED BOOST!", textPosition, Color.cyan);
        }

        Debug.Log($"Joueur {collector.playerID} a ramassé un Speed Boost !");
    }

    private void ApplySpeedSlowToOtherPlayer(PlayerInput collector)
    {
        // Trouve l'autre joueur
        PlayerInput[] allPlayers = FindObjectsOfType<PlayerInput>();

        foreach (PlayerInput player in allPlayers)
        {
            // Si c'est un autre joueur (pas celui qui ramasse)
            if (player.playerID != collector.playerID)
            {
                PlayerPowerUpManager targetPowerUpManager = player.GetComponent<PlayerPowerUpManager>();
                if (targetPowerUpManager != null)
                {
                    // Applique le ralentissement
                    targetPowerUpManager.ApplySpeedSlow();

                    Debug.Log($"Joueur {collector.playerID} a ralenti le joueur {player.playerID} !");
                }
            }
        }

        // Texte flottant pour le ramasseur
        if (enableFloatingText)
        {
            Vector3 textPosition = transform.position + Vector3.up * 0.5f;
            FloatingText.Create("ENEMY SLOWED!", textPosition, Color.red);
        }
    }

    private void PlayCollectionEffects()
    {
        // 1. Camera Shake
        if (enableCameraShake)
        {
            if (CameraShakeManager.Instance != null)
            {
                CameraShakeManager.Instance.ShakeAllLight();
            }
            else if (CameraShake.Instance != null)
            {
                CameraShake.Instance.ShakeLight();
            }
        }

        // 2. Son de collecte
        if (enableCollectSound && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySoundAtPosition(collectSoundName, transform.position);
        }

        // 3. Effet de particules
        if (collectEffect != null)
        {
            Instantiate(collectEffect, transform.position, Quaternion.identity);
        }

        // 4. Pulse rapide
        if (objectPulse != null)
        {
            objectPulse.PulseOnce(1.3f, 0.15f);
        }
    }
}
