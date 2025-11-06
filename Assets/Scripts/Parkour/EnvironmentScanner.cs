using UnityEngine;

[RequireComponent(typeof(PlayerInput))] 
public class EnvironmentScanner : MonoBehaviour
{
    [Header("Détection Glissade (Devant)")]
    public Transform slideCheckOrigin; 
    public float slideCheckDistance = 0.8f;

    [Header("Détection Plafond (Dessus)")]
    public Transform standUpCheckOrigin; 
    public float standUpCheckDistance = 1.1f; 
    
    [Header("Détection Franchissement (Vault)")]
    public Transform vaultCheckOrigin_Low;  
    public Transform vaultCheckOrigin_High; 
    public float vaultCheckDistance = 3.0f; // C'est la distance MAXIMALE
    public float minVaultDistance = 0.5f; // <-- NOUVELLE VARIABLE (la distance MINIMALE)
    
    [Header("Configuration")]
    public LayerMask obstacleLayer;

    // Propriétés
    public bool CanSlide { get; private set; }
    public bool IsObstacleAbove { get; private set; } 
    public bool CanVault { get; private set; } 

    public Vector3 currentDirection = Vector3.right; 
    private PlayerInput playerInput;

    void Start()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    void Update()
    {
        // On met à jour la direction dans laquelle on scanne
        float horizontalInput = playerInput.HorizontalInput;
        if (horizontalInput > 0.1f)
        {
            currentDirection = Vector3.right;
        }
        else if (horizontalInput < -0.1f)
        {
            currentDirection = Vector3.left;
        }

        // --- (Logique de Glissade et Plafond, inchangée) ---
        CanSlide = Physics.Raycast(
            slideCheckOrigin.position, currentDirection, slideCheckDistance, obstacleLayer);
        
        IsObstacleAbove = Physics.Raycast(
            standUpCheckOrigin.position, Vector3.up, standUpCheckDistance, obstacleLayer);
        
        // ==========================================================
        // === LOGIQUE DE FRANCHISSEMENT (MODIFIÉE) ===
        // ==========================================================
        
        // 1. On vérifie en bas (aux genoux)
        bool obstacleBelow = false; // On commence en supposant qu'on ne peut pas
        RaycastHit vaultHit; // Variable pour stocker les infos du rayon

        if (Physics.Raycast(
            vaultCheckOrigin_Low.position, 
            currentDirection, 
            out vaultHit, // Stocke l'info de ce qui a été touché
            vaultCheckDistance, // Distance MAX
            obstacleLayer))
        {
            // On a touché quelque chose ! Mais est-ce à la bonne distance ?
            // On vérifie si la distance est PLUS GRANDE que le minimum requis.
            if (vaultHit.distance > minVaultDistance)
            {
                obstacleBelow = true; // C'est bon, on est dans la "zone de confort"
            }
            // Si la distance est plus petite que 'minVaultDistance', 'obstacleBelow'
            // reste 'false', et le franchissement ne se déclenchera pas.
        }
        
        // 2. On vérifie en haut (à la tête) qu'il n'y a PAS d'obstacle
        // (Cette logique ne change pas)
        bool spaceAbove = !Physics.Raycast( 
            vaultCheckOrigin_High.position, 
            currentDirection, 
            vaultCheckDistance, 
            obstacleLayer
        );
        
        // La condition finale
        CanVault = obstacleBelow && spaceAbove;
    }

    void OnDrawGizmosSelected()
    {
        // (Le reste de votre code OnDrawGizmosSelected, qui utilise déjà 'currentDirection')
        if (slideCheckOrigin != null)
        {
            Gizmos.color = CanSlide ? Color.green : Color.red;
            Gizmos.DrawRay(slideCheckOrigin.position, currentDirection * slideCheckDistance); 
        }
        
        if (standUpCheckOrigin != null)
        {
            Gizmos.color = IsObstacleAbove ? Color.yellow : Color.blue;
            Gizmos.DrawRay(standUpCheckOrigin.position, Vector3.up * standUpCheckDistance);
        }
        
        if (vaultCheckOrigin_Low != null && vaultCheckOrigin_High != null)
        {
            Gizmos.color = CanVault ? Color.cyan : Color.red;
            Gizmos.DrawRay(vaultCheckOrigin_Low.position, currentDirection * vaultCheckDistance); 
            Gizmos.DrawRay(vaultCheckOrigin_High.position, currentDirection * vaultCheckDistance); 
        }
    }
}