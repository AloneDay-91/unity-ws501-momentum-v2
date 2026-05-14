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

#if !WEB_BUILD
    // --- Arcade / single-player path: local time-based integration --------------
    private Vector3 targetPosition;
    private bool movingToEnd = true;
    private float pauseTimer = 0f;
    private bool isPaused = false;
    private float delayTimer = 0f;
    private bool hasStarted = false;

    void Start()
    {
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
        if (!hasStarted)
        {
            StopMoveSound();
            delayTimer -= Time.deltaTime;
            if (delayTimer <= 0f) hasStarted = true;
            return;
        }

        if (isPaused)
        {
            StopMoveSound();
            pauseTimer -= Time.deltaTime;
            if (pauseTimer <= 0f)
            {
                isPaused = false;
                movingToEnd = !movingToEnd;
                targetPosition = movingToEnd ? endPoint : startPoint;
            }
            return;
        }

        PlayMoveSound();
        MovePlatform();
    }

    void MovePlatform()
    {
        Vector3 currentPos = transform.localPosition;
        float distance = Vector3.Distance(currentPos, targetPosition);

        if (distance < 0.01f)
        {
            transform.localPosition = targetPosition;
            if (pauseDuration > 0f)
            {
                isPaused = true;
                pauseTimer = pauseDuration;
            }
            else
            {
                movingToEnd = !movingToEnd;
                targetPosition = movingToEnd ? endPoint : startPoint;
            }
            return;
        }

        float step = speed * Time.deltaTime;
        Vector3 newPosition = (movementType == MovementType.Smooth)
            ? Vector3.Lerp(currentPos, targetPosition, step / distance)
            : Vector3.MoveTowards(currentPos, targetPosition, step);
        transform.localPosition = newPosition;
    }

    void OnCollisionStay(Collision collision)
    {
        if (!movePlayersWithPlatform) return;
        if (!collision.gameObject.CompareTag("Player")) return;
        if (collision.contacts.Length == 0) return;

        Vector3 normal = collision.contacts[0].normal;
        if (Vector3.Dot(normal, Vector3.down) > 0.5f)
        {
            Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                Vector3 platformVelocity = (targetPosition - transform.localPosition).normalized * speed;
                playerRb.velocity = new Vector3(platformVelocity.x, playerRb.velocity.y, platformVelocity.z);
            }
        }
    }
#endif

#if WEB_BUILD
    // --- Multiplayer path: position = pure function of shared server clock ----
    // Both clients sample WebMatchClock.MatchTime (anchored to GameState.elapsedTime)
    // so ComputePhase produces byte-identical world positions everywhere, which is
    // what keeps the two clients visually in sync without per-platform broadcast.
    private Vector3 _cachedLocalVelocity;

    void Start()
    {
        // Pre-game pose so the platform isn't sitting at world origin while we wait
        // for the first server elapsedTime tick.
        transform.localPosition = startAtEndPoint ? endPoint : startPoint;
    }

    void Update()
    {
        if (WebMatchClock.Instance == null || !WebMatchClock.Instance.HasStarted)
        {
            // Hold pre-game pose until the match really starts on the server.
            transform.localPosition = startAtEndPoint ? endPoint : startPoint;
            _cachedLocalVelocity = Vector3.zero;
            StopMoveSound();
            return;
        }

        ComputePhase(WebMatchClock.Instance.MatchTime, out Vector3 pos, out Vector3 vel);
        transform.localPosition = pos;
        _cachedLocalVelocity = vel;

        if (vel.sqrMagnitude > 0.0001f) PlayMoveSound();
        else StopMoveSound();
    }

    private void ComputePhase(float matchTime, out Vector3 position, out Vector3 velocity)
    {
        Vector3 a = startAtEndPoint ? endPoint : startPoint;
        Vector3 b = startAtEndPoint ? startPoint : endPoint;
        position = a;
        velocity = Vector3.zero;

        float t = matchTime - initialDelay;
        if (t < 0f) return;

        float dist = Vector3.Distance(startPoint, endPoint);
        if (dist < 0.0001f) return;

        float legDuration = dist / Mathf.Max(0.0001f, speed);
        float cycle = 2f * (legDuration + pauseDuration);
        if (cycle < 0.0001f) return;

        float u = t - Mathf.Floor(t / cycle) * cycle;

        if (u < legDuration)
        {
            // a → b
            float k = u / legDuration;
            if (movementType == MovementType.Smooth) k = Mathf.SmoothStep(0f, 1f, k);
            position = Vector3.Lerp(a, b, k);
            velocity = (b - a).normalized * speed;
        }
        else if (u < legDuration + pauseDuration)
        {
            position = b;
        }
        else if (u < 2f * legDuration + pauseDuration)
        {
            // b → a
            float k = (u - legDuration - pauseDuration) / legDuration;
            if (movementType == MovementType.Smooth) k = Mathf.SmoothStep(0f, 1f, k);
            position = Vector3.Lerp(b, a, k);
            velocity = (a - b).normalized * speed;
        }
        else
        {
            position = a;
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (!movePlayersWithPlatform) return;
        if (!collision.gameObject.CompareTag("Player")) return;
        if (collision.contacts.Length == 0) return;

        Vector3 normal = collision.contacts[0].normal;
        if (Vector3.Dot(normal, Vector3.down) <= 0.5f) return;

        Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();
        if (playerRb == null) return;

        // _cachedLocalVelocity is local-space (computed against startPoint/endPoint).
        // Convert to world for the rigidbody push.
        Vector3 worldVel = transform.parent != null
            ? transform.parent.TransformVector(_cachedLocalVelocity)
            : _cachedLocalVelocity;
        playerRb.velocity = new Vector3(worldVel.x, playerRb.velocity.y, worldVel.z);
    }
#endif

    void PlayMoveSound()
    {
        if (moveAudioSource != null && !moveAudioSource.isPlaying) moveAudioSource.Play();
    }

    void StopMoveSound()
    {
        if (moveAudioSource != null && moveAudioSource.isPlaying) moveAudioSource.Stop();
    }

    // Visualisation dans l'éditeur
    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = Color.green;
        Vector3 worldStart = transform.parent != null ? transform.parent.TransformPoint(startPoint) : startPoint;
        Gizmos.DrawWireSphere(worldStart, 0.3f);

        Gizmos.color = Color.red;
        Vector3 worldEnd = transform.parent != null ? transform.parent.TransformPoint(endPoint) : endPoint;
        Gizmos.DrawWireSphere(worldEnd, 0.3f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(worldStart, worldEnd);
    }
}
