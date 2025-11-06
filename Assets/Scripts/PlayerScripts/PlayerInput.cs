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
    
    [Header("Configuration Joueur")]
    [Tooltip("Mettre 1 pour le Joueur 1, 2 pour le Joueur 2")]
    public int playerID = 1; 

    [Header("Buffer d'Input")]
    public float jumpBufferDuration = 0.2f; // Durée de la mémoire (en sec)
    private float jumpBufferTimer; // Le chronomètre
    
    // Noms des axes (privés)
    private string horizontalAxisName;
    private string verticalAxisName;
    private string jumpButtonName;
    private string slideButtonName;

    void Start()
    {
        // Construit les noms des axes en fonction de l'ID du joueur
        horizontalAxisName = "P" + playerID + "_Horizontal";
        verticalAxisName = "P" + playerID + "_Vertical";
        jumpButtonName = "P" + playerID + "_B1"; // Bouton 1 pour Sauter
        slideButtonName = "P" + playerID + "_B2"; // Bouton 2 pour Glisser
    }
    
    // Fonction publique pour "consommer" le saut
    public void ConsumeJumpBuffer()
    {
        jumpBufferTimer = 0;
    }

    void Update()
    {
        // --- LOGIQUE DU BUFFER DE SAUT ---
        // 1. On fait descendre le chrono
        if (jumpBufferTimer > 0)
        {
            jumpBufferTimer -= Time.deltaTime;
        }

        // 2. Si on appuie, on active le buffer
        if (Input.GetButtonDown(jumpButtonName))
        {
            jumpBufferTimer = jumpBufferDuration;
        }
        // --- FIN LOGIQUE DU BUFFER ---

        // Lecture des autres inputs
        HorizontalInput = Input.GetAxis(horizontalAxisName);
        VerticalInput = Input.GetAxis(verticalAxisName);
        
        SlidePressed = Input.GetButtonDown(slideButtonName);
        SlideHeld = Input.GetButton(slideButtonName);
    }
}