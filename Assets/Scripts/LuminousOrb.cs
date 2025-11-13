using UnityEngine;

// On s'assure que les composants sont là
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(Collider))]
public class LuminousOrb : MonoBehaviour
{
    public int luminescenceAmount = 10; // Quantité de lumière donnée
    public GameObject collectEffect; // (Optionnel) Effet de particule à jouer

    // On garde en mémoire les composants
    private MeshRenderer meshRenderer;
    private Collider orbCollider;

    void Awake()
    {
        // On récupère les composants au réveil
        meshRenderer = GetComponent<MeshRenderer>();
        orbCollider = GetComponent<Collider>();
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
    }

    void HideOrb()
    {
        // On désactive le visuel et le collider
        // L'objet et ce script restent actifs !
        meshRenderer.enabled = false;
        orbCollider.enabled = false;
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
            
            // (Optionnel) Jouer un son ou un effet
            if (collectEffect != null)
            {
                Instantiate(collectEffect, transform.position, Quaternion.identity);
            }
            
            // On se cache (en attendant la prochaine phase jour)
            HideOrb();
        }
    }
}