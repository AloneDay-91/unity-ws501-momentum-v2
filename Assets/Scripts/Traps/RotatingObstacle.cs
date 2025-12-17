using UnityEngine;

/// <summary>
/// Obstacle qui tourne sur lui-même
/// Peut être un moulin, des lames rotatives, etc.
/// </summary>
public class RotatingObstacle : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Axe de rotation (X, Y, Z)")]
    public Vector3 rotationAxis = Vector3.up;

    [Tooltip("Vitesse de rotation (degrés par seconde)")]
    [Range(-360f, 360f)]
    public float rotationSpeed = 45f;

    [Header("Timing")]
    [Tooltip("Délai avant de commencer la rotation")]
    [Range(0f, 10f)]
    public float initialDelay = 0f;

    [Tooltip("Pause périodique (0 = pas de pause)")]
    [Range(0f, 10f)]
    public float pauseInterval = 0f;

    [Tooltip("Durée de la pause")]
    [Range(0f, 5f)]
    public float pauseDuration = 1f;

    [Header("Advanced")]
    [Tooltip("Utiliser l'espace local (true) ou world (false)")]
    public bool useLocalSpace = true;

    [Tooltip("Inverser la direction périodiquement")]
    public bool reverseDirection = false;

    [Tooltip("Intervalle d'inversion (en secondes, 0 = pas d'inversion)")]
    [Range(0f, 20f)]
    public float reverseInterval = 0f;

    private float delayTimer = 0f;
    private bool hasStarted = false;
    private float pauseTimer = 0f;
    private bool isPaused = false;
    private float reverseTimer = 0f;
    private float currentSpeed;

    void Start()
    {
        delayTimer = initialDelay;
        hasStarted = initialDelay <= 0f;
        currentSpeed = rotationSpeed;
        reverseTimer = reverseInterval;
    }

    void Update()
    {
        // Délai initial
        if (!hasStarted)
        {
            delayTimer -= Time.deltaTime;
            if (delayTimer <= 0f)
            {
                hasStarted = true;
            }
            return;
        }

        // Gestion de la pause périodique
        if (pauseInterval > 0f)
        {
            if (isPaused)
            {
                pauseTimer -= Time.deltaTime;
                if (pauseTimer <= 0f)
                {
                    isPaused = false;
                    pauseTimer = pauseInterval;
                }
                return;
            }
            else
            {
                pauseTimer -= Time.deltaTime;
                if (pauseTimer <= 0f)
                {
                    isPaused = true;
                    pauseTimer = pauseDuration;
                    return;
                }
            }
        }

        // Gestion de l'inversion de direction
        if (reverseDirection && reverseInterval > 0f)
        {
            reverseTimer -= Time.deltaTime;
            if (reverseTimer <= 0f)
            {
                currentSpeed = -currentSpeed;
                reverseTimer = reverseInterval;
            }
        }

        // Rotation
        Vector3 rotation = rotationAxis.normalized * currentSpeed * Time.deltaTime;

        if (useLocalSpace)
        {
            transform.Rotate(rotation, Space.Self);
        }
        else
        {
            transform.Rotate(rotation, Space.World);
        }
    }
}
