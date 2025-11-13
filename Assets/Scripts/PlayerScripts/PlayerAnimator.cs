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

    // Fonction publique appelée par PlayerMovement
    public void TriggerJump()
    {
        if (animator != null)
        {
            animator.SetTrigger("doJump");
        }
    }

    void Update()
    {
        if (animator == null) return;

        // 1. Vitesse de Mouvement
        animator.SetFloat("moveSpeed", Mathf.Abs(playerInput.HorizontalInput));
        
        // 2. État "au sol" (lit la version "faussée" pour l'animation)
        animator.SetBool("isGrounded", playerMovement.isGrounded_ForAnimator);
        
        // 3. Logique de Glissade (Pente uniquement)
        bool physicsSlide = playerMovement.IsInSlopeZone;
        animator.SetBool("isSliding", physicsSlide);
        
        // 4. Atterrissage brutal (Roulade)
        animator.SetBool("isLandingHard", playerMovement.isLandingHard);
        
        // 5. Action "Sauter" (Logique déplacée dans PlayerMovement)
        
        // 6. Logique de Retournement (Flip)
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