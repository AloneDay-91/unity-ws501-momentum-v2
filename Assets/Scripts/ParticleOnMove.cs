using UnityEngine;

/// <summary>
/// Émet des particules quand l'objet se déplace (trail de poussière, etc.)
/// Attache ce script au joueur et assigne un Particle System
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ParticleOnMove : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Le système de particules à contrôler")]
    public ParticleSystem moveParticles;

    [Header("Movement Detection")]
    [Tooltip("Vitesse minimum pour émettre des particules")]
    [Range(0.1f, 5f)]
    public float minSpeedToEmit = 0.5f;

    [Tooltip("Taux d'émission maximum (particules/seconde)")]
    [Range(1f, 100f)]
    public float maxEmissionRate = 30f;

    [Header("Ground Detection")]
    [Tooltip("Le joueur doit être au sol pour émettre")]
    public bool requireGrounded = true;

    [Tooltip("Distance du raycast pour détecter le sol")]
    public float groundCheckDistance = 0.2f;

    [Tooltip("Layer du sol")]
    public LayerMask groundLayer = -1;

    private Rigidbody rb;
    private ParticleSystem.EmissionModule emission;
    private bool isGrounded;
    private Vector3 lastPosition;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Créer ou récupérer le particle system
        if (moveParticles == null)
        {
            moveParticles = GetComponentInChildren<ParticleSystem>();
        }

        if (moveParticles != null)
        {
            emission = moveParticles.emission;
            emission.rateOverTime = 0; // Commence à 0
        }
        else
        {
            Debug.LogWarning("Aucun Particle System assigné à ParticleOnMove !");
        }

        lastPosition = transform.position;
    }

    void Update()
    {
        if (moveParticles == null) return;

        // Détection du sol
        if (requireGrounded)
        {
            CheckGround();
        }
        else
        {
            isGrounded = true; // Toujours considéré comme au sol si non requis
        }

        // Calcul de la vitesse
        float currentSpeed = rb.velocity.magnitude;

        // Calcul du taux d'émission basé sur la vitesse
        if (isGrounded && currentSpeed > minSpeedToEmit)
        {
            // Map la vitesse au taux d'émission
            float normalizedSpeed = Mathf.Clamp01((currentSpeed - minSpeedToEmit) / 5f);
            float emissionRate = normalizedSpeed * maxEmissionRate;
            emission.rateOverTime = emissionRate;

            // Active le système si pas déjà actif
            if (!moveParticles.isPlaying)
            {
                moveParticles.Play();
            }
        }
        else
        {
            // Arrête l'émission si trop lent ou en l'air
            emission.rateOverTime = 0;

            // Arrête complètement le système après un délai
            if (moveParticles.isPlaying && !moveParticles.isEmitting)
            {
                moveParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        lastPosition = transform.position;
    }

    private void CheckGround()
    {
        // Raycast vers le bas pour détecter le sol
        RaycastHit hit;
        Vector3 origin = transform.position;
        Vector3 direction = -transform.up;

        isGrounded = Physics.Raycast(origin, direction, out hit, groundCheckDistance, groundLayer);

        // Optionnel: Dessiner le raycast en mode debug
        if (isGrounded)
        {
            Debug.DrawRay(origin, direction * groundCheckDistance, Color.green);
        }
        else
        {
            Debug.DrawRay(origin, direction * groundCheckDistance, Color.red);
        }
    }

    /// <summary>
    /// Émet un burst de particules (pour le saut, atterrissage, etc.)
    /// </summary>
    public void EmitBurst(int particleCount = 20)
    {
        if (moveParticles != null)
        {
            moveParticles.Emit(particleCount);
        }
    }

    /// <summary>
    /// Active/désactive l'émission de particules manuellement
    /// </summary>
    public void SetEmissionEnabled(bool enabled)
    {
        if (moveParticles != null)
        {
            if (enabled)
            {
                moveParticles.Play();
            }
            else
            {
                moveParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }

    // Visualisation dans l'éditeur
    void OnDrawGizmosSelected()
    {
        if (requireGrounded)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawRay(transform.position, -transform.up * groundCheckDistance);
        }
    }
}
