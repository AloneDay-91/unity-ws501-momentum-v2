using UnityEngine;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(ParkourController))]
[RequireComponent(typeof(EnvironmentScanner))] 
public class PlayerAnimator : MonoBehaviour
{
    // Références
    private Animator animator;
    private PlayerInput playerInput;
    private PlayerMovement playerMovement;
    private ParkourController parkourController;
    private EnvironmentScanner scanner; 
    private Rigidbody rb; 
    
    private Transform modelTransform; 
    private Quaternion facingRight;
    private Quaternion facingLeft;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        playerInput = GetComponent<PlayerInput>();
        playerMovement = GetComponent<PlayerMovement>();
        parkourController = GetComponent<ParkourController>();
        scanner = GetComponent<EnvironmentScanner>(); 
        rb = GetComponent<Rigidbody>(); 

        modelTransform = animator.transform; 
        facingRight = Quaternion.Euler(0, 90, 0); 
        facingLeft = Quaternion.Euler(0, -90, 0);
    }

    // --- CETTE FONCTION EST CORRIGÉE ---
    void Update()
    {
        if (animator == null) return;

        // 1. Vitesse de Mouvement
        animator.SetFloat("moveSpeed", Mathf.Abs(playerInput.HorizontalInput));
        
        // 2. État "au sol"
        animator.SetBool("isGrounded", playerMovement.isGrounded);
        
        // --- LOGIQUE DE GLISSADE CORRIGÉE ---
        // Ce script ne gère plus que la glissade de PENTE (automatique).
        // La glissade MANUELLE est gérée par le ParkourController avec un Trigger.
        
        bool physicsSlide = playerMovement.IsInSlopeZone;
        animator.SetBool("isSliding", physicsSlide);
        // --- FIN DE LA CORRECTION ---
        
        // 4. Action "Sauter"
        if (playerInput.JumpBufferActive && playerMovement.isGrounded && !scanner.CanVault)
        {
            animator.SetTrigger("doJump");
        }
        
        // 5. Logique de Retournement (Flip)
        HandleFlipping();
    }
    
    private void HandleFlipping()
    {
        float moveInput = playerInput.HorizontalInput;

        if (moveInput > 0.1f) 
        {
            modelTransform.localRotation = facingRight;
        }
        else if (moveInput < -0.1f)
        {
            modelTransform.localRotation = facingLeft;
        }
    }
}