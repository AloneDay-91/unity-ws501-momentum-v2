using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    // Propriétés lues par les autres scripts
    public float HorizontalInput { get; private set; }
    public float VerticalInput { get; private set; }
    public bool SlidePressed { get; private set; }
    public bool SlideHeld { get; private set; }
    
    // Le buffer de saut
    public bool JumpBufferActive { get { return jumpBufferTimer > 0; } }
    
    // --- NOUVELLE VARIABLE ---
    public bool LightTogglePressed { get; private set; } // Bouton pour la lumière
    // --- FIN ---

    [Header("Configuration Joueur")]
    [Tooltip("Mettre 1 pour le Joueur 1, 2 pour le Joueur 2")]
    public int playerID = 1; 

    [Header("Buffer d'Input")]
    public float jumpBufferDuration = 0.2f; 
    private float jumpBufferTimer; 
    
    // Noms des axes (privés)
    private string horizontalAxisName;
    private string verticalAxisName;
    private string jumpButtonName;
    private string slideButtonName;
    private string lightButtonName; // <-- NOUVEAU

    void Start()
    {
        horizontalAxisName = "P" + playerID + "_Horizontal";
        verticalAxisName = "P" + playerID + "_Vertical";
        jumpButtonName = "P" + playerID + "_B1"; 
        slideButtonName = "P" + playerID + "_B2"; 
        lightButtonName = "P" + playerID + "_B3"; // <-- NOUVEAU
    }
    
    // Fonction publique pour "consommer" le saut
    public void ConsumeJumpBuffer()
    {
        jumpBufferTimer = 0;
    }

    void Update()
    {
        // --- LOGIQUE DU BUFFER DE SAUT ---
        if (jumpBufferTimer > 0)
        {
            jumpBufferTimer -= Time.deltaTime;
        }

        if (Input.GetButtonDown(jumpButtonName))
        {
            jumpBufferTimer = jumpBufferDuration;
        }
        
        // --- LECTURE DES INPUTS ---
        HorizontalInput = Input.GetAxis(horizontalAxisName);
        VerticalInput = Input.GetAxis(verticalAxisName);
        
        SlidePressed = Input.GetButtonDown(slideButtonName);
        SlideHeld = Input.GetButton(slideButtonName);
        
        LightTogglePressed = Input.GetButtonDown(lightButtonName); // <-- NOUVEAU
    }
}