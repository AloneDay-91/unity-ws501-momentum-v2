using UnityEngine;

/// <summary>
/// Gère les particules de poussière pour le joueur (course, glisse, saut, atterrissage)
/// Attachez ce script au joueur et assignez les systèmes de particules
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerDustParticles : MonoBehaviour
{
    [Header("Particle Systems")]
    [Tooltip("Particules lors de la course/glisse")]
    public ParticleSystem runDustParticles;

    [Tooltip("Particules lors du saut")]
    public ParticleSystem jumpParticles;

    [Tooltip("Particules lors de l'atterrissage")]
    public ParticleSystem landParticles;

    [Header("Run/Slide Settings")]
    [Tooltip("Vitesse minimum pour émettre des particules de course")]
    [Range(0.1f, 5f)]
    public float minSpeedToEmit = 1f;

    [Tooltip("Taux d'émission maximum (particules/seconde)")]
    [Range(1f, 100f)]
    public float maxEmissionRate = 30f;

    [Tooltip("Particules de glisse (plus de particules)")]
    [Range(1f, 3f)]
    public float slideParticleMultiplier = 2f;

    [Header("Jump/Land Settings")]
    [Tooltip("Nombre de particules lors du saut")]
    [Range(5, 50)]
    public int jumpBurstCount = 15;

    [Tooltip("Nombre de particules lors de l'atterrissage")]
    [Range(10, 100)]
    public int landBurstCount = 30;

    [Header("Ground Detection")]
    [Tooltip("Distance du raycast pour détecter le sol")]
    public float groundCheckDistance = 0.3f;

    [Tooltip("Layer du sol")]
    public LayerMask groundLayer = -1;

    [Tooltip("Offset du point de spawn des particules")]
    public Vector3 particleSpawnOffset = Vector3.zero;

    [Header("Debug")]
    [Tooltip("Activer les logs de debug dans la console")]
    public bool debugMode = false;

    [Tooltip("Afficher les gizmos en play mode")]
    public bool showDebugGizmos = true;

    private Rigidbody rb;
    private ParticleSystem.EmissionModule runEmission;
    private bool isGrounded;
    private bool wasGrounded;
    private float currentSpeed;
    private bool isSliding;
    private float debugTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Configuration du système de particules de course
        if (runDustParticles != null)
        {
            runEmission = runDustParticles.emission;
            runEmission.rateOverTime = 0;
        }
        else
        {
            Debug.LogWarning("PlayerDustParticles: Aucun système de particules de course assigné !");
        }
    }

    void Update()
    {
        // Détection du sol
        CheckGround();

        // Calcul de la vitesse horizontale
        Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        currentSpeed = horizontalVelocity.magnitude;

        // Gestion des particules de course/glisse
        HandleRunDust();

        // Détection du saut et atterrissage
        HandleJumpAndLanding();

        // Debug logs (tous les 0.5 secondes)
        if (debugMode)
        {
            debugTimer += Time.deltaTime;
            if (debugTimer >= 0.5f)
            {
                Debug.Log($"[PlayerDust] Grounded: {isGrounded} | Speed: {currentSpeed:F2} | EmissionRate: {(runDustParticles != null ? runEmission.rateOverTime.constant : 0):F1}");
                debugTimer = 0f;
            }
        }

        // Mise à jour de l'état précédent
        wasGrounded = isGrounded;
    }

    private void CheckGround()
    {
        Vector3 origin = transform.position + particleSpawnOffset;
        Vector3 direction = -transform.up;

        isGrounded = Physics.Raycast(origin, direction, out RaycastHit hit, groundCheckDistance, groundLayer);

        // Debug visualization
        Debug.DrawRay(origin, direction * groundCheckDistance, isGrounded ? Color.green : Color.red);
    }

    private void HandleRunDust()
    {
        if (runDustParticles == null) return;

        // Si au sol et en mouvement
        if (isGrounded && currentSpeed > minSpeedToEmit)
        {
            // Calcul du taux d'émission basé sur la vitesse
            float normalizedSpeed = Mathf.Clamp01((currentSpeed - minSpeedToEmit) / 5f);
            float emissionRate = normalizedSpeed * maxEmissionRate;

            // Si en glisse, augmenter l'émission
            if (isSliding)
            {
                emissionRate *= slideParticleMultiplier;
            }

            runEmission.rateOverTime = emissionRate;

            if (!runDustParticles.isPlaying)
            {
                runDustParticles.Play();
            }
        }
        else
        {
            // Arrête l'émission
            runEmission.rateOverTime = 0;

            if (runDustParticles.isPlaying && !runDustParticles.isEmitting)
            {
                runDustParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }

    private void HandleJumpAndLanding()
    {
        // Détection du saut (passage de au sol à en l'air)
        if (wasGrounded && !isGrounded && rb.velocity.y > 0)
        {
            OnJump();
        }

        // Détection de l'atterrissage (passage de en l'air à au sol)
        if (!wasGrounded && isGrounded)
        {
            OnLand();
        }
    }

    private void OnJump()
    {
        if (jumpParticles != null)
        {
            Vector3 spawnPos = transform.position + particleSpawnOffset;
            jumpParticles.transform.position = spawnPos;
            jumpParticles.Emit(jumpBurstCount);
        }
    }

    private void OnLand()
    {
        if (landParticles != null)
        {
            Vector3 spawnPos = transform.position + particleSpawnOffset;
            landParticles.transform.position = spawnPos;

            // Plus de particules si atterrissage rapide
            int burstCount = landBurstCount;
            if (Mathf.Abs(rb.velocity.y) > 5f)
            {
                burstCount = (int)(landBurstCount * 1.5f); // Augmente de 50% pour chute rapide
            }

            landParticles.Emit(burstCount);
        }
    }

    /// <summary>
    /// Active/désactive le mode glisse (plus de particules)
    /// À appeler depuis le script de mouvement du joueur
    /// </summary>
    public void SetSliding(bool sliding)
    {
        isSliding = sliding;
    }

    /// <summary>
    /// Force un burst de particules de course
    /// </summary>
    public void EmitRunBurst(int count = 20)
    {
        if (runDustParticles != null)
        {
            runDustParticles.Emit(count);
        }
    }

    /// <summary>
    /// Force un jump burst
    /// </summary>
    public void ForceJumpParticles()
    {
        OnJump();
    }

    /// <summary>
    /// Force un land burst
    /// </summary>
    public void ForceLandParticles()
    {
        OnLand();
    }

    // Visualisation dans l'éditeur ET en play mode
    void OnDrawGizmos()
    {
        if (!showDebugGizmos && !Application.isPlaying) return;
        if (!showDebugGizmos && Application.isPlaying) return;

        Vector3 origin = transform.position + particleSpawnOffset;

        // Raycast de détection du sol
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawRay(origin, -transform.up * groundCheckDistance);
        Gizmos.DrawWireSphere(origin, 0.1f);

        // Indicateur de vitesse
        if (Application.isPlaying)
        {
            Gizmos.color = currentSpeed > minSpeedToEmit ? Color.cyan : Color.gray;
            Vector3 velocityDir = new Vector3(rb.velocity.x, 0, rb.velocity.z).normalized;
            Gizmos.DrawRay(transform.position, velocityDir * currentSpeed);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Zone de spawn des particules
        Vector3 origin = transform.position + particleSpawnOffset;
        Gizmos.color = new Color(1, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(origin, 0.3f);
    }
}
