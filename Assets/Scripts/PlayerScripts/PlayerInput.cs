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
        // Input axes: there's only ONE local player on the web client, so it always uses the
        // P1_* input axes regardless of whether the server assigned it as P1 or P2. Game-side
        // identity (death attribution, score tracking, UI matching) keeps `playerID` intact —
        // before, we hard-coded playerID=1 here too, which made Player_J2 misreport itself as P1
        // and broke per-player elimination on the P2 client.
        int axisID = playerID;
#if WEB_BUILD
        axisID = 1;
#endif
        horizontalAxisName = "P" + axisID + "_Horizontal";
        verticalAxisName = "P" + axisID + "_Vertical";
        jumpButtonName = "P" + axisID + "_B1";
        slideButtonName = "P" + axisID + "_B2";
        lightButtonName = "P" + axisID + "_B3";
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