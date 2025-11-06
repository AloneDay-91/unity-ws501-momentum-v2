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
        // 2. Logique de Glissade Manuelle
        else if (playerInput.SlidePressed && !isManuallySliding && !isVaulting && !playerMovement.IsInSlopeZone)
        {
            isManuallySliding = true; 
            StartCoroutine(PerformSlide());
        }
    }

    IEnumerator PerformSlide()
    {
        //playerStats.AddScoreForAction("Slide");
        
        if (animator != null)
        {
            animator.SetTrigger("doManualSlide");
        }
        
        playerCollider.direction = 1; 
        playerCollider.height = slideHeight;
        playerCollider.center = new Vector3(0, slideColliderCenterY, 0); 
        
        yield return new WaitForSeconds(slideDuration);
        
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
        
        // --- LA CORRECTION EST ICI ---
        // On arrête la vélocité AVANT de passer en Kinematic
        rb.velocity = Vector3.zero;
        rb.isKinematic = true; 
        // --- FIN DE LA CORRECTION ---
        
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