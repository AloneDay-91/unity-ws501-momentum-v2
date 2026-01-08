using UnityEngine;

/// <summary>
/// Plateforme ou obstacle qui se déplace entre deux points
/// Peut être une plateforme mobile, un mur qui se déplace, etc.
/// </summary>
public class MovingPlatform : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Point de départ (local position)")]
    public Vector3 startPoint = Vector3.zero;

    [Tooltip("Point d'arrivée (local position)")]
    public Vector3 endPoint = new Vector3(10, 0, 0);

    [Tooltip("Vitesse de déplacement")]
    [Range(0.1f, 20f)]
    public float speed = 2f;

    [Tooltip("Temps de pause aux extrémités (en secondes)")]
    [Range(0f, 10f)]
    public float pauseDuration = 1f;

    [Header("Audio")]
    public AudioSource moveAudioSource;

    [Header("Timing")]
    [Tooltip("Délai avant de commencer le mouvement")]
    [Range(0f, 10f)]
    public float initialDelay = 0f;

    [Tooltip("Commencer au point de départ ou d'arrivée?")]
    public bool startAtEndPoint = false;

    [Header("Movement Type")]
    [Tooltip("Type de mouvement (Linear = constant, Smooth = accélération/décélération)")]
    public MovementType movementType = MovementType.Smooth;

    [Header("Behavior")]
    [Tooltip("Déplacer les joueurs qui sont sur la plateforme")]
    public bool movePlayersWithPlatform = true;

    [Header("Debug")]
    [Tooltip("Afficher les points de départ/arrivée dans l'éditeur")]
    public bool showGizmos = true;

    public enum MovementType
    {
        Linear,     // Vitesse constante
        Smooth      // Accélération/décélération (SmoothStep)
    }

    private Vector3 targetPosition;
    private bool movingToEnd = true;
    private float pauseTimer = 0f;
    private bool isPaused = false;
    private float delayTimer = 0f;
    private bool hasStarted = false;

    void Start()
    {
        // Position initiale
        if (startAtEndPoint)
        {
            transform.localPosition = endPoint;
            movingToEnd = false;
            targetPosition = startPoint;
        }
        else
        {
            transform.localPosition = startPoint;
            movingToEnd = true;
            targetPosition = endPoint;
        }

        delayTimer = initialDelay;
        hasStarted = initialDelay <= 0f;
    }

    void Update()
    {
        // Délai initial
        if (!hasStarted)
        {
            StopMoveSound();
            delayTimer -= Time.deltaTime;
            if (delayTimer <= 0f)
            {
                hasStarted = true;
            }
            return;
        }

        // Pause aux extrémités
        if (isPaused)
        {
            StopMoveSound();
            pauseTimer -= Time.deltaTime;
            if (pauseTimer <= 0f)
            {
                isPaused = false;
                // Change de direction
                movingToEnd = !movingToEnd;
                targetPosition = movingToEnd ? endPoint : startPoint;
            }
            return;
        }

        // Déplacement
        PlayMoveSound();
        MovePlatform();
    }

    void PlayMoveSound()
    {
        if (moveAudioSource != null && !moveAudioSource.isPlaying)
        {
            moveAudioSource.Play();
        }
    }

    void StopMoveSound()
    {
        if (moveAudioSource != null && moveAudioSource.isPlaying)
        {
            moveAudioSource.Stop();
        }
    }

    void MovePlatform()
    {
        Vector3 currentPos = transform.localPosition;
        float distance = Vector3.Distance(currentPos, targetPosition);

        if (distance < 0.01f)
        {
            // Arrivé à destination
            transform.localPosition = targetPosition;

            // Commence la pause
            if (pauseDuration > 0f)
            {
                isPaused = true;
                pauseTimer = pauseDuration;
            }
            else
            {
                // Pas de pause, change de direction directement
                movingToEnd = !movingToEnd;
                targetPosition = movingToEnd ? endPoint : startPoint;
            }
            return;
        }

        // Calcule le mouvement
        float step = speed * Time.deltaTime;

        Vector3 newPosition;
        if (movementType == MovementType.Smooth)
        {
            // Mouvement smooth (SmoothStep)
            newPosition = Vector3.Lerp(currentPos, targetPosition, step / distance);
        }
        else
        {
            // Mouvement linéaire
            newPosition = Vector3.MoveTowards(currentPos, targetPosition, step);
        }

        transform.localPosition = newPosition;
    }

    // Déplace les joueurs avec la plateforme
    void OnCollisionStay(Collision collision)
    {
        if (!movePlayersWithPlatform) return;

        // Si c'est un joueur sur la plateforme
        if (collision.gameObject.CompareTag("Player"))
        {
            // Vérifie que le joueur est au-dessus
            if (collision.contacts.Length > 0)
            {
                Vector3 normal = collision.contacts[0].normal;
                if (Vector3.Dot(normal, Vector3.down) > 0.5f) // Player is on top
                {
                    // Déplace le joueur avec la plateforme
                    Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();
                    if (playerRb != null)
                    {
                        Vector3 platformVelocity = (targetPosition - transform.localPosition).normalized * speed;
                        playerRb.velocity = new Vector3(platformVelocity.x, playerRb.velocity.y, platformVelocity.z);
                    }
                }
            }
        }
    }

    // Visualisation dans l'éditeur
    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // Point de départ (vert)
        Gizmos.color = Color.green;
        Vector3 worldStart = transform.parent != null ? transform.parent.TransformPoint(startPoint) : startPoint;
        Gizmos.DrawWireSphere(worldStart, 0.3f);

        // Point d'arrivée (rouge)
        Gizmos.color = Color.red;
        Vector3 worldEnd = transform.parent != null ? transform.parent.TransformPoint(endPoint) : endPoint;
        Gizmos.DrawWireSphere(worldEnd, 0.3f);

        // Ligne entre les deux points
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(worldStart, worldEnd);
    }
}