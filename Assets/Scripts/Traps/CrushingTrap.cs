using UnityEngine;

/// <summary>
/// Piège qui écrase (piston, presse)
/// Se déplace vers le bas puis remonte, peut tuer le joueur si coincé
/// </summary>
public class CrushingTrap : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Distance de descente (en unités)")]
    [Range(0.5f, 20f)]
    public float crushDistance = 5f;

    [Tooltip("Vitesse de descente")]
    [Range(0.1f, 20f)]
    public float crushSpeed = 3f;

    [Tooltip("Vitesse de remontée")]
    [Range(0.1f, 20f)]
    public float returnSpeed = 2f;

    [Header("Timing")]
    [Tooltip("Temps en haut avant de descendre")]
    [Range(0f, 10f)]
    public float topPauseDuration = 2f;

    [Tooltip("Temps en bas avant de remonter")]
    [Range(0f, 10f)]
    public float bottomPauseDuration = 1f;

    [Tooltip("Délai initial avant de commencer")]
    [Range(0f, 10f)]
    public float initialDelay = 0f;

    [Header("Damage")]
    [Tooltip("Infliger des dégâts aux joueurs écrasés")]
    public bool dealDamage = true;

    [Tooltip("Montant de dégâts (ou instant kill si très élevé)")]
    public int damageAmount = 100;

    [Header("Effects")]
    [Tooltip("Son de descente (nom dans AudioManager)")]
    public string crushSoundName = "crush";

    [Tooltip("Son d'impact (quand ça touche le bas)")]
    public string impactSoundName = "impact";

    [Tooltip("Shake de caméra à l'impact")]
    public bool shakeOnImpact = true;

    [Header("Debug")]
    public bool showGizmos = true;

    private enum State
    {
        WaitingAtTop,
        Crushing,
        WaitingAtBottom,
        Returning
    }

    private State currentState = State.WaitingAtTop;
    private Vector3 topPosition;
    private Vector3 bottomPosition;
    private float stateTimer = 0f;
    private bool hasStarted = false;

    void Start()
    {
        // Mémorise la position du haut
        topPosition = transform.localPosition;
        bottomPosition = topPosition - new Vector3(0, crushDistance, 0);

        stateTimer = initialDelay;
        hasStarted = initialDelay <= 0f;

        if (hasStarted)
        {
            stateTimer = topPauseDuration;
        }
    }

    void Update()
    {
        // Délai initial
        if (!hasStarted)
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                hasStarted = true;
                currentState = State.WaitingAtTop;
                stateTimer = topPauseDuration;
            }
            return;
        }

        switch (currentState)
        {
            case State.WaitingAtTop:
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                {
                    // Commence à descendre
                    currentState = State.Crushing;
                    PlaySound(crushSoundName);
                }
                break;

            case State.Crushing:
                // Descend
                transform.localPosition = Vector3.MoveTowards(
                    transform.localPosition,
                    bottomPosition,
                    crushSpeed * Time.deltaTime
                );

                // Arrivé en bas ?
                if (Vector3.Distance(transform.localPosition, bottomPosition) < 0.01f)
                {
                    transform.localPosition = bottomPosition;
                    currentState = State.WaitingAtBottom;
                    stateTimer = bottomPauseDuration;

                    // Impact
                    OnReachedBottom();
                }
                break;

            case State.WaitingAtBottom:
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                {
                    // Commence à remonter
                    currentState = State.Returning;
                }
                break;

            case State.Returning:
                // Remonte
                transform.localPosition = Vector3.MoveTowards(
                    transform.localPosition,
                    topPosition,
                    returnSpeed * Time.deltaTime
                );

                // Arrivé en haut ?
                if (Vector3.Distance(transform.localPosition, topPosition) < 0.01f)
                {
                    transform.localPosition = topPosition;
                    currentState = State.WaitingAtTop;
                    stateTimer = topPauseDuration;
                }
                break;
        }
    }

    void OnReachedBottom()
    {
        // Son d'impact
        PlaySound(impactSoundName);

        // Shake de caméra
        if (shakeOnImpact && CameraShakeManager.Instance != null)
        {
            CameraShakeManager.Instance.ShakeAllMedium();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Si on écrase un joueur pendant la descente
        if (currentState == State.Crushing && dealDamage)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                ApplyDamageToPlayer(collision.gameObject);
            }
        }
    }

    void OnCollisionStay(Collision collision)
    {
        // Continue d'écraser le joueur coincé
        if (currentState == State.Crushing && dealDamage)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                // Optionnel : dégâts continus
                // ApplyDamageToPlayer(collision.gameObject);
            }
        }
    }

    private void ApplyDamageToPlayer(GameObject player)
    {
        // Tu peux implémenter un système de vie ici
        // Pour l'instant, on va juste téléporter le joueur ou l'écraser

        Debug.Log($"Joueur {player.name} écrasé par le piège!");

        // Option 1: Téléporter à un point de respawn
        // player.GetComponent<PlayerRespawn>()?.Respawn();

        // Option 2: Appliquer des dégâts
        // PlayerHealth health = player.GetComponent<PlayerHealth>();
        // if (health != null)
        // {
        //     health.TakeDamage(damageAmount);
        // }

        // Pour l'instant, affiche juste un message
        FloatingText.Create("ÉCRASÉ!", player.transform.position + Vector3.up, Color.red);
    }

    private void PlaySound(string soundName)
    {
        if (!string.IsNullOrEmpty(soundName) && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySoundAtPosition(soundName, transform.position);
        }
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Vector3 top = Application.isPlaying ? topPosition : transform.localPosition;
        Vector3 bottom = Application.isPlaying ? bottomPosition : top - new Vector3(0, crushDistance, 0);

        // Position haute (vert)
        Gizmos.color = Color.green;
        Vector3 worldTop = transform.parent != null ? transform.parent.TransformPoint(top) : top;
        Gizmos.DrawWireCube(worldTop, transform.localScale);

        // Position basse (rouge)
        Gizmos.color = Color.red;
        Vector3 worldBottom = transform.parent != null ? transform.parent.TransformPoint(bottom) : bottom;
        Gizmos.DrawWireCube(worldBottom, transform.localScale);

        // Ligne de trajet
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(worldTop, worldBottom);
    }
}
