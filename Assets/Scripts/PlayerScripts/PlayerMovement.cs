using UnityEngine;
using System.Collections; // Important pour les Coroutines

[RequireComponent(typeof(Rigidbody))] 
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(EnvironmentScanner))] 
[RequireComponent(typeof(CapsuleCollider))] 
public class PlayerMovement : MonoBehaviour
{
    [Header("Mouvement")]
    public float moveSpeed = 10f;

    [Header("Saut")]
    public float jumpForce = 7f; 
    public float groundCheckDistance = 1.1f; 
    public LayerMask groundLayer;
    
    [Tooltip("Temps de CHUTE (secondes) avant de déclencher une roulade.")]
    public float hardLandThreshold = 0.8f; 
    
    [Header("Buffer")]
    public float groundedBufferDuration = 0.1f;
    private float groundedBufferTimer; 
    [Tooltip("Durée (en secondes) pendant laquelle la roulade est active.")]
    public float hardLandDuration = 0.2f;
    private float hardLandTimer = 0f;

    [Header("Pente")]
    public float slopeSlideForce = 15f; 
    public GameObject slopeWallPrefab; 

    // Références
    private Rigidbody rb;
    private PlayerInput playerInput;
    private EnvironmentScanner scanner;
    private CapsuleCollider playerCollider; 
    private ParkourController parkourController; 
    private PlayerAnimator playerAnimator; 

    // États
    public bool IsInSlopeZone { get; private set; } = false;
    public bool isLandingHard { get; private set; } = false; 
    private float airTime = 0f; 
    public bool isGrounded_ForAnimator { get; private set; } 
    public bool isGrounded_Buffered { get; private set; }
    public bool IsPhysicallyGrounded { get; private set; } 
    private bool isStunned = false; // <-- NOUVEAU

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();
        scanner = GetComponent<EnvironmentScanner>(); 
        playerCollider = GetComponent<CapsuleCollider>(); 
        parkourController = GetComponent<ParkourController>(); 
        playerAnimator = GetComponent<PlayerAnimator>(); 
    }

    void Update()
    {
        // Si on est étourdi, on ne peut pas mettre à jour la logique
        if (isStunned)
        {
            isLandingHard = false;
            return;
        }

        if (hardLandTimer > 0) { hardLandTimer -= Time.deltaTime; }
        isLandingHard = hardLandTimer > 0;
        
        bool onGroundNow = Physics.Raycast(transform.position, Vector3.down, out RaycastHit groundHit, groundCheckDistance, groundLayer);
        IsPhysicallyGrounded = onGroundNow;

        if (parkourController.isManuallySliding)
        {
            isGrounded_ForAnimator = true;
            airTime = 0f; 
        }
        else
        {
            isGrounded_ForAnimator = onGroundNow;
        }

        if (onGroundNow) 
        {
            if (!isGrounded_Buffered && airTime > hardLandThreshold)
            {
                hardLandTimer = hardLandDuration; 
                parkourController.LockSlideAfterRoll();
            }
            groundedBufferTimer = groundedBufferDuration;
            airTime = 0f; 
        }
        else 
        {
            groundedBufferTimer -= Time.deltaTime; 
            if (rb.velocity.y < 0) { airTime += Time.deltaTime; }
            else { airTime = 0f; }
        }
        isGrounded_Buffered = groundedBufferTimer > 0;
        
        if (playerInput.JumpBufferActive && isGrounded_Buffered && !scanner.CanVault && !parkourController.isManuallySliding)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            if (playerAnimator != null) { playerAnimator.TriggerJump(); }
            playerInput.ConsumeJumpBuffer();
        }
    }

    void FixedUpdate()
    {
        // Si on est étourdi, on ne peut pas bouger
        if (isStunned)
        {
            // On freine le joueur
            rb.velocity = new Vector3(Mathf.Lerp(rb.velocity.x, 0, Time.fixedDeltaTime * 10f), rb.velocity.y, rb.velocity.z);
            return; // On ignore tout le reste
        }
        
        float horizontalInput = playerInput.HorizontalInput;
        
        if (IsInSlopeZone && isGrounded_ForAnimator)
        {
            // Logique de pente
        }
        else
        {
            // Logique de mouvement normal
            Vector3 newVelocity = rb.velocity;
            if (Mathf.Abs(horizontalInput) > 0.1f)
            {
                newVelocity.x = horizontalInput * moveSpeed;
            }
            else 
            {
                newVelocity.x = Mathf.Lerp(rb.velocity.x, 0, Time.fixedDeltaTime * 10f);
            }
            rb.velocity = new Vector3(newVelocity.x, rb.velocity.y, rb.velocity.z);
        }

        if (IsInSlopeZone && isGrounded_ForAnimator)
        {
            rb.AddForce(Vector3.right * slopeSlideForce, ForceMode.Force); 
            rb.AddForce(Vector3.down * 10f, ForceMode.Force);
        }
    }
    
    // --- NOUVELLE FONCTION PUBLIQUE ---
    public void ApplyStun(float duration)
    {
        // On ne peut pas être étourdi si on est déjà étourdi ou en action
        if (isStunned || parkourController.isManuallySliding) return;

        StartCoroutine(StunCoroutine(duration));
    }

    // --- NOUVELLE COROUTINE ---
    private IEnumerator StunCoroutine(float duration)
    {
        isStunned = true;
        
        // (Optionnel) Dit à l'Animator de jouer l'animation "touché"
        if (playerAnimator != null)
        {
            // Vous devrez créer un Trigger "doStun" dans l'Animator
            // playerAnimator.animator.SetTrigger("doStun"); 
        }

        yield return new WaitForSeconds(duration);

        isStunned = false;
    }
    
    // --- (Le reste du script : Triggers, OnDrawGizmos, etc. ne change pas) ---
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("SlopeZone"))
        {
            IsInSlopeZone = true;
        }
        
        if (other.CompareTag("SlopeExit"))
        {
            if (slopeWallPrefab != null)
            {
                slopeWallPrefab.SetActive(true);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("SlopeZone"))
        {
            IsInSlopeZone = false;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, Vector3.down * groundCheckDistance);
    }
}