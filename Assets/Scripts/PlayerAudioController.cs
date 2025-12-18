using UnityEngine;

/// <summary>
/// Gère tous les sons du personnage (pas, slide, vault, etc.)
/// Attache ce script sur ton GameObject joueur (Player_J1, Player_J2)
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(ParkourController))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerAudioController : MonoBehaviour
{
    [Header("Sound Names (configure dans AudioManager)")]
    [Tooltip("Nom du son de pas dans l'AudioManager")]
    public string footstepSoundName = "footstep";

    [Tooltip("Nom du son de slide dans l'AudioManager")]
    public string slideSoundName = "slide";

    [Tooltip("Nom du son de vault dans l'AudioManager")]
    public string vaultSoundName = "vault";

    [Tooltip("Nom du son de saut dans l'AudioManager")]
    public string jumpSoundName = "jump";

    [Tooltip("Nom du son d'atterrissage dans l'AudioManager")]
    public string landSoundName = "land";

    [Header("Footstep Settings")]
    [Tooltip("Vitesse minimale pour jouer les sons de pas")]
    public float minSpeedForFootsteps = 0.5f;

    [Tooltip("Intervalle entre chaque son de pas (en secondes)")]
    public float footstepInterval = 0.4f;

    [Tooltip("Activer les sons de pas")]
    public bool enableFootsteps = true;

    [Header("Other Settings")]
    [Tooltip("Activer les sons d'actions (slide, vault, jump)")]
    public bool enableActionSounds = true;

    // Références
    private PlayerMovement playerMovement;
    private ParkourController parkourController;
    private Rigidbody rb;
    private AudioManager audioManager;

    // État interne
    private float footstepTimer = 0f;
    private bool wasGrounded = false;
    private bool wasSliding = false;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        parkourController = GetComponent<ParkourController>();
        rb = GetComponent<Rigidbody>();

        // Trouve l'AudioManager
        audioManager = AudioManager.Instance;
        if (audioManager == null)
        {
            Debug.LogWarning("PlayerAudioController: AudioManager introuvable ! Les sons ne seront pas joués.");
        }
    }

    void Update()
    {
        if (audioManager == null) return;

        // 1. Gestion des sons de pas
        HandleFootsteps();

        // 2. Détection du début du slide
        HandleSlideSound();

        // 3. Détection de l'atterrissage
        HandleLandingSound();
    }

    private void HandleFootsteps()
    {
        if (!enableFootsteps) return;

        // Joue des pas seulement si :
        // - Le joueur est au sol
        // - Le joueur bouge assez vite
        // - Le joueur ne glisse pas
        bool shouldPlayFootsteps = playerMovement.isGrounded_ForAnimator &&
                                   !parkourController.isManuallySliding &&
                                   Mathf.Abs(rb.velocity.x) > minSpeedForFootsteps;

        if (shouldPlayFootsteps)
        {
            footstepTimer += Time.deltaTime;

            if (footstepTimer >= footstepInterval)
            {
                audioManager.PlaySound(footstepSoundName);
                footstepTimer = 0f;
            }
        }
        else
        {
            // Réinitialise le timer si le joueur s'arrête
            footstepTimer = 0f;
        }
    }

    private void HandleSlideSound()
    {
        if (!enableActionSounds) return;

        // Détecte le début du slide
        bool isSliding = parkourController.isManuallySliding;

        if (isSliding && !wasSliding)
        {
            // Le slide vient de commencer
            audioManager.PlaySound(slideSoundName);
        }

        wasSliding = isSliding;
    }

    private void HandleLandingSound()
    {
        if (!enableActionSounds) return;

        // Détecte l'atterrissage
        bool isGrounded = playerMovement.isGrounded_ForAnimator;

        if (isGrounded && !wasGrounded && rb.velocity.y < -2f)
        {
            // Le joueur vient d'atterrir avec une certaine force
            audioManager.PlaySound(landSoundName);
        }

        wasGrounded = isGrounded;
    }

    /// <summary>
    /// Appelle cette fonction quand le joueur saute (depuis PlayerMovement)
    /// </summary>
    public void PlayJumpSound()
    {
        if (audioManager != null && enableActionSounds)
        {
            audioManager.PlaySound(jumpSoundName);
        }
    }

    /// <summary>
    /// Appelle cette fonction quand le joueur fait un vault (depuis ParkourController)
    /// </summary>
    public void PlayVaultSound()
    {
        if (audioManager != null && enableActionSounds)
        {
            audioManager.PlaySound(vaultSoundName);
        }
    }

    /// <summary>
    /// Arrête tous les sons du joueur
    /// </summary>
    public void StopAllSounds()
    {
        if (audioManager != null)
        {
            // On pourrait arrêter les sons en boucle ici si nécessaire
            footstepTimer = 0f;
        }
    }
}
