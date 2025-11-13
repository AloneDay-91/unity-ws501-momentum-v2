using UnityEngine;
using System.Collections;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(EnvironmentScanner))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(Rigidbody))] 
//[RequireComponent(typeof(PlayerStats))]
public class ParkourController : MonoBehaviour
{
    [Header("Paramètres de Glissade")]
    public float slideHeight = 1.0f; 
    public float slideDuration = 1.5f; 
    [Tooltip("La position Y du centre du collider pendant la glissade.")]
    public float slideColliderCenterY = -0.5f; 

    [Header("Paramètres de Franchissement")]
    public float vaultSpeed = 3.0f; 
    public float vaultDuration = 1.17f; 
    public float vaultHopHeight = 0.8f; 

    [Header("Réglages de Gameplay")]
    [Tooltip("La durée (en secondes) pendant laquelle la glissade est bloquée après une roulade.")]
    public float slideLockoutDuration = 0.5f; 

    // Références
    private PlayerInput playerInput;
    private EnvironmentScanner scanner;
    private CapsuleCollider playerCollider;
    private PlayerMovement playerMovement;
    private Rigidbody rb; 
    //private PlayerStats playerStats;
    private Animator animator; 

    // Valeurs de base
    private float originalColliderHeight;
    private Vector3 originalColliderCenter;
    
    // États
    private bool isVaulting = false; 
    public bool isManuallySliding { get; private set; } = false; 
    private bool isSlideLocked = false; 

    void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        scanner = GetComponent<EnvironmentScanner>();
        playerCollider = GetComponent<CapsuleCollider>();
        playerMovement = GetComponent<PlayerMovement>();
        rb = GetComponent<Rigidbody>(); 
        //playerStats = GetComponent<PlayerStats>();
        animator = GetComponentInChildren<Animator>(); 
        
        originalColliderHeight = playerCollider.height;
        originalColliderCenter = playerCollider.center;
    }

    void Update()
    {
        // 1. Priorité au Franchissement
        if (scanner.CanVault && playerInput.JumpBufferActive && !isVaulting && !isManuallySliding)
        {
            playerInput.ConsumeJumpBuffer(); 
            StartCoroutine(PerformVault());
        }
        
        // 2. Logique de Glissade Manuelle (utilise 'isGrounded_ForAnimator')
        else if (playerInput.SlidePressed && !isManuallySliding && !isVaulting && !playerMovement.IsInSlopeZone && playerMovement.isGrounded_ForAnimator && !isSlideLocked)
        {
            isManuallySliding = true; 
            StartCoroutine(PerformSlide());
        }
    }

    // Fonction publique appelée par PlayerMovement
    public void LockSlideAfterRoll()
    {
        StartCoroutine(SlideLockoutCoroutine());
    }

    // Coroutine de verrouillage
    private IEnumerator SlideLockoutCoroutine()
    {
        isSlideLocked = true; 
        yield return new WaitForSeconds(slideLockoutDuration); 
        isSlideLocked = false;
    }

    IEnumerator PerformSlide()
    {
        //isManuallySliding = true; // (Déplacé dans Update)
        //playerStats.AddScoreForAction("Slide");
        
        if (animator != null)
        {
            animator.SetTrigger("doManualSlide");
        }
        
        playerCollider.direction = 1; 
        playerCollider.height = slideHeight;
        playerCollider.center = new Vector3(0, slideColliderCenterY, 0); 
        
        yield return new WaitForSeconds(slideDuration);
        
        // --- NOUVELLE LOGIQUE DE SÉCURITÉ ---
        // On vérifie la VÉRITÉ PHYSIQUE avant de se relever
        if (playerMovement.IsPhysicallyGrounded)
        {
            RestoreCollider();
        }
        else
        {
            // On est en l'air ! On attend de toucher le sol.
            StartCoroutine(WaitForGroundToStopSlide());
        }
    }
    
    IEnumerator WaitForGroundToStopSlide()
    {
        // On attend (boucle) jusqu'à ce que le joueur touche VRAIMENT le sol
        while (!playerMovement.IsPhysicallyGrounded)
        {
            yield return null; // Attend la prochaine frame
        }
        
        // Le joueur a touché le sol. On peut maintenant se relever.
        RestoreCollider();
    }
    
    void RestoreCollider()
    {
        playerCollider.direction = 1; 
        playerCollider.height = originalColliderHeight;
        playerCollider.center = originalColliderCenter;
        
        isManuallySliding = false; 
    }
    
    IEnumerator PerformVault()
    {
        isVaulting = true;
        //playerStats.AddScoreForAction("Vault");
        
        if (animator != null)
        {
            animator.SetTrigger("doVault");
        }
        
        playerMovement.enabled = false;
        rb.useGravity = false;
        
        rb.velocity = Vector3.zero;
        rb.isKinematic = true; 
        
        float timer = 0f;
        while (timer < vaultDuration)
        {
            transform.Translate(scanner.currentDirection * vaultSpeed * Time.deltaTime, Space.World);
            
            float hopSpeed = vaultHopHeight;
            if (timer < vaultDuration / 2)
            {
                transform.Translate(Vector3.up * hopSpeed * Time.deltaTime, Space.World);
            }
            else
            {
                transform.Translate(Vector3.down * hopSpeed * Time.deltaTime, Space.World);
            }

            timer += Time.deltaTime;
            yield return null; 
        }
        
        isVaulting = false;
        playerMovement.enabled = true;
        rb.useGravity = true;
        rb.isKinematic = false;
    }
}