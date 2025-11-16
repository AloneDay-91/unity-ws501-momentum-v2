using UnityEngine;

// On s'assure que les composants sont là
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(Collider))]
public class LuminousOrb : MonoBehaviour
{
    [Header("Collecte")]
    public int luminescenceAmount = 10; // Quantité de lumière donnée
    public GameObject collectEffect; // (Optionnel) Effet de particule à jouer

    [Header("Particules")]
    [Tooltip("Système de particules permanent autour de l'orbe")]
    public ParticleSystem orbParticles;

    [Header("Juice Effects")]
    [Tooltip("Activer le camera shake lors de la collecte")]
    public bool enableCameraShake = true;

    [Tooltip("Activer le texte flottant (+10)")]
    public bool enableFloatingText = true;

    [Tooltip("Couleur du texte flottant")]
    public Color floatingTextColor = Color.yellow;

    [Tooltip("Nom du son de collecte (dans AudioManager)")]
    public string collectSoundName = "collect";

    [Tooltip("Activer le son de collecte")]
    public bool enableCollectSound = true;

    [Header("Collection Bar")]
    [Tooltip("Incrémenter la barre de collection (optionnel)")]
    public bool updateCollectionBar = true;

    [Tooltip("Nombre d'orbs à ajouter à la barre (1 = 1 orb)")]
    public int orbBarValue = 1;

    // On garde en mémoire les composants
    private MeshRenderer meshRenderer;
    private Collider orbCollider;
    private ObjectPulse objectPulse;

    void Awake()
    {
        // On récupère les composants au réveil
        meshRenderer = GetComponent<MeshRenderer>();
        orbCollider = GetComponent<Collider>();
        objectPulse = GetComponent<ObjectPulse>();

        // Si pas assigné dans l'inspecteur, on cherche dans les enfants
        if (orbParticles == null)
        {
            orbParticles = GetComponentInChildren<ParticleSystem>();
        }

        // Ajoute ObjectPulse si pas déjà présent (pour l'effet de pulsation)
        if (objectPulse == null)
        {
            objectPulse = gameObject.AddComponent<ObjectPulse>();
        }
    }

    // S'abonne aux événements
    void OnEnable()
    {
        GameCycleManager.OnDayStart += ShowOrb;
        GameCycleManager.OnNightStart += HideOrb;
    }

    // Se désabonne
    void OnDisable()
    {
        GameCycleManager.OnDayStart -= ShowOrb;
        GameCycleManager.OnNightStart -= HideOrb;
    }

    // Vérifie l'état au démarrage
    void Start()
    {
        if (GameCycleManager.Instance.IsDay)
        {
            ShowOrb();
        }
        else
        {
            HideOrb();
        }
    }

    // --- FONCTIONS CORRIGÉES ---

    void ShowOrb()
    {
        // On active le visuel et le collider
        meshRenderer.enabled = true;
        orbCollider.enabled = true;

        // On active les particules si elles existent
        if (orbParticles != null)
        {
            orbParticles.Play();
        }
    }

    void HideOrb()
    {
        // On désactive le visuel et le collider
        // L'objet et ce script restent actifs !
        meshRenderer.enabled = false;
        orbCollider.enabled = false;

        // On arrête les particules si elles existent
        if (orbParticles != null)
        {
            orbParticles.Stop();
        }
    }

    // Quand un joueur touche l'orbe
    void OnTriggerEnter(Collider other)
    {
        // Si l'orbe est cachée (de nuit), le collider est désactivé,
        // donc cette fonction ne peut pas être appelée. C'est parfait.

        PlayerStats playerStats = other.GetComponent<PlayerStats>();

        if (playerStats != null)
        {
            // On donne la luminescence au joueur
            playerStats.AddLuminescence(luminescenceAmount);

            // --- EFFETS DE JUICE ---

            // 1. Camera Shake (toutes les caméras)
            if (enableCameraShake)
            {
                // Essaie d'abord le manager multi-caméras
                if (CameraShakeManager.Instance != null)
                {
                    CameraShakeManager.Instance.ShakeAllLight();
                }
                // Fallback sur une seule caméra si pas de manager
                else if (CameraShake.Instance != null)
                {
                    CameraShake.Instance.ShakeLight();
                }
            }

            // 2. Floating Text (+10)
            if (enableFloatingText)
            {
                Vector3 textPosition = transform.position + Vector3.up * 0.5f;
                FloatingText.Create($"+{luminescenceAmount}", textPosition, floatingTextColor);
            }

            // 3. Son de collecte
            if (enableCollectSound && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySoundAtPosition(collectSoundName, transform.position);
            }

            // 4. Effet de particules (déjà existant)
            if (collectEffect != null)
            {
                Instantiate(collectEffect, transform.position, Quaternion.identity);
            }

            // 5. Pulse rapide avant de disparaître
            if (objectPulse != null)
            {
                objectPulse.PulseOnce(1.3f, 0.15f);
            }

            // 6. Incrémente la barre de collection
            if (updateCollectionBar)
            {
                OrbCollectionBar collectionBar = FindObjectOfType<OrbCollectionBar>();
                if (collectionBar != null)
                {
                    collectionBar.AddOrbs(orbBarValue);
                }
            }

            // On se cache (en attendant la prochaine phase jour)
            HideOrb();
        }
    }
}