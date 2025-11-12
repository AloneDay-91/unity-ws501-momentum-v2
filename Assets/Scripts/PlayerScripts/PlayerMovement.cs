using UnityEngine;

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
    public float hardLandThreshold = 0.8f; // (C'est votre valeur)
    
    [Header("Buffer")]
    public float groundedBufferDuration = 0.1f;
    private float groundedBufferTimer; 

    [Header("Pente")]
    public float slopeSlideForce = 15f; 
    public GameObject slopeWallPrefab; 

    // Références
    private Rigidbody rb;
    private PlayerInput playerInput;
    private EnvironmentScanner scanner;
    private CapsuleCollider playerCollider; 
    private ParkourController parkourController; 

    // États
    public bool isGrounded { get; private set; }
    public bool IsInSlopeZone { get; private set; } = false;
    
    // Cette variable est lue par PlayerAnimator
    public bool isLandingHard { get; private set; } = false; 
    private float airTime = 0f; // Compteur de temps en l'air

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();
        scanner = GetComponent<EnvironmentScanner>(); 
        playerCollider = GetComponent<CapsuleCollider>(); 
        parkourController = GetComponent<ParkourController>(); 
    }

    void Update()
    {
        // On réinitialise 'isLandingHard' à chaque frame
        isLandingHard = false;
        
        if (parkourController.isManuallySliding)
        {
            isGrounded = true;
            airTime = 0f; 
        }
        else 
        {
            bool onGroundNow = Physics.Raycast(transform.position, Vector3.down, out RaycastHit groundHit, groundCheckDistance, groundLayer);

            if (onGroundNow) // Si on est au sol
            {
                // On vient JUSTE d'atterrir
                if (!isGrounded && airTime > hardLandThreshold)
                {
                    isLandingHard = true; // Déclenche la roulade !
                }
                
                groundedBufferTimer = groundedBufferDuration;
                airTime = 0f; 
            }
            else // --- ON EST EN L'AIR ---
            {
                groundedBufferTimer -= Time.deltaTime;
                
                // On ne compte le temps en l'air QUE SI on TOMBE (vitesse Y négative)
                if (rb.velocity.y < 0)
                {
                    airTime += Time.deltaTime; 
                }
                else
                {
                    airTime = 0f; // Si on MONTE (saut), on ne compte pas
                }
            }
            isGrounded = groundedBufferTimer > 0;
        }
        
        // Logique de Saut
        if (playerInput.JumpBufferActive && isGrounded && !scanner.CanVault)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            playerInput.ConsumeJumpBuffer();
        }
    }

    void FixedUpdate()
    {
        float horizontalInput = playerInput.HorizontalInput;
        
        if (IsInSlopeZone && isGrounded)
        {
            // Logique de pente (ignore l'input)
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

        if (IsInSlopeZone && isGrounded)
        {
            rb.AddForce(Vector3.right * slopeSlideForce, ForceMode.Force); 
            rb.AddForce(Vector3.down * 10f, ForceMode.Force);
        }
    }
    
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