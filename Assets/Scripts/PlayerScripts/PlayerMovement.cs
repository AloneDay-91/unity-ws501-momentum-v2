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
    
    [Header("Buffer")]
    public float groundedBufferDuration = 0.1f;
    private float groundedBufferTimer; 

    [Header("Pente")]
    [Tooltip("La force qui pousse le joueur le long de la pente.")]
    public float slopeSlideForce = 15f; 
    
    // NOUVELLE VARIABLE
    [Header("Logique de Barrière")]
    [Tooltip("Faites glisser l'objet 'SlopeExitWall' de votre scène ici.")]
    public GameObject slopeWallPrefab; // Le mur à activer

    // Références
    private Rigidbody rb;
    private PlayerInput playerInput;
    private EnvironmentScanner scanner;
    private CapsuleCollider playerCollider; 
    private ParkourController parkourController; 

    // États
    public bool isGrounded { get; private set; }
    public bool IsInSlopeZone { get; private set; } = false;

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
        // Si on fait une glissade manuelle, on force "isGrounded"
        if (parkourController.isManuallySliding)
        {
            isGrounded = true;
        }
        else // Détection normale du sol
        {
            bool onGroundNow = Physics.Raycast(transform.position, Vector3.down, out RaycastHit groundHit, groundCheckDistance, groundLayer);

            if (onGroundNow)
            {
                groundedBufferTimer = groundedBufferDuration;
            }
            else
            {
                groundedBufferTimer -= Time.deltaTime;
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
        
        // PRIORITÉ 1: EST-ON SUR UNE PENTE ?
        if (IsInSlopeZone && isGrounded)
        {
            // OUI. L'INPUT EST IGNORÉ.
            // On laisse les 'AddForce' (plus bas) gérer 100% du mouvement.
        }
        // PRIORITÉ 2: PAS SUR UNE PENTE (Mouvement normal)
        else
        {
            Vector3 newVelocity = rb.velocity;
            if (Mathf.Abs(horizontalInput) > 0.1f)
            {
                // Le joueur contrôle
                newVelocity.x = horizontalInput * moveSpeed;
            }
            else // Pas d'input, pas de pente -> Freiner
            {
                newVelocity.x = Mathf.Lerp(rb.velocity.x, 0, Time.fixedDeltaTime * 10f);
            }
            // On applique la vélocité SI on n'est PAS sur la pente
            rb.velocity = new Vector3(newVelocity.x, rb.velocity.y, rb.velocity.z);
        }

        // GESTION DE LA PHYSIQUE DE PENTE (SÉPARÉMENT)
        if (IsInSlopeZone && isGrounded)
        {
            // Applique la force de glisse (vers la droite)
            rb.AddForce(Vector3.right * slopeSlideForce, ForceMode.Force); 
            
            // Applique la force pour coller au sol
            rb.AddForce(Vector3.down * 10f, ForceMode.Force);
        }
    }
    
    // --- FONCTION MODIFIÉE ---
    void OnTriggerEnter(Collider other)
    {
        // 1. Logique de la Zone de Pente
        if (other.gameObject.layer == LayerMask.NameToLayer("SlopeZone"))
        {
            IsInSlopeZone = true;
        }
        
        // 2. NOUVELLE LOGIQUE : Activation de la Barrière
        // Si on touche l'objet qui a le Tag "SlopeExit"
        if (other.CompareTag("SlopeExit"))
        {
            // On vérifie que le mur a bien été assigné
            if (slopeWallPrefab != null)
            {
                // On active le mur !
                slopeWallPrefab.SetActive(true);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Si on sort d'un objet sur le layer "SlopeZone"
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