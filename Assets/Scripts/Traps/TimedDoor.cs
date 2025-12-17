using UnityEngine;

/// <summary>
/// Porte ou mur qui s'ouvre et se ferme automatiquement selon un timer
/// Peut bloquer le passage du joueur de manière périodique
/// </summary>
public class TimedDoor : MonoBehaviour
{
    [Header("Door Settings")]
    [Tooltip("La porte commence ouverte ou fermée?")]
    public bool startOpen = false;

    [Tooltip("Temps en position ouverte (secondes)")]
    [Range(0.5f, 20f)]
    public float openDuration = 3f;

    [Tooltip("Temps en position fermée (secondes)")]
    [Range(0.5f, 20f)]
    public float closedDuration = 3f;

    [Tooltip("Vitesse d'ouverture/fermeture")]
    [Range(0.1f, 20f)]
    public float moveSpeed = 2f;

    [Header("Movement")]
    [Tooltip("Position fermée (local)")]
    public Vector3 closedPosition = Vector3.zero;

    [Tooltip("Position ouverte (local) - direction d'ouverture")]
    public Vector3 openPosition = new Vector3(0, 5, 0);

    [Header("Behavior")]
    [Tooltip("Écraser les joueurs si la porte se ferme sur eux")]
    public bool crushPlayers = false;

    [Tooltip("Bloquer la fermeture si un joueur est dans le chemin")]
    public bool blockIfPlayerInWay = true;

    [Header("Effects")]
    [Tooltip("Son d'ouverture")]
    public string openSoundName = "door_open";

    [Tooltip("Son de fermeture")]
    public string closeSoundName = "door_close";

    [Header("Debug")]
    public bool showGizmos = true;

    private enum DoorState
    {
        Opening,
        Open,
        Closing,
        Closed
    }

    private DoorState currentState;
    private float stateTimer = 0f;
    private bool playerInWay = false;

    void Start()
    {
        // Position initiale
        if (startOpen)
        {
            transform.localPosition = openPosition;
            currentState = DoorState.Open;
            stateTimer = openDuration;
        }
        else
        {
            transform.localPosition = closedPosition;
            currentState = DoorState.Closed;
            stateTimer = closedDuration;
        }
    }

    void Update()
    {
        stateTimer -= Time.deltaTime;

        switch (currentState)
        {
            case DoorState.Closed:
                if (stateTimer <= 0f)
                {
                    // Commence à s'ouvrir
                    currentState = DoorState.Opening;
                    PlaySound(openSoundName);
                }
                break;

            case DoorState.Opening:
                // Mouvement vers position ouverte
                transform.localPosition = Vector3.MoveTowards(
                    transform.localPosition,
                    openPosition,
                    moveSpeed * Time.deltaTime
                );

                // Arrivée en position ouverte ?
                if (Vector3.Distance(transform.localPosition, openPosition) < 0.01f)
                {
                    transform.localPosition = openPosition;
                    currentState = DoorState.Open;
                    stateTimer = openDuration;
                }
                break;

            case DoorState.Open:
                if (stateTimer <= 0f)
                {
                    // Commence à se fermer
                    currentState = DoorState.Closing;
                    PlaySound(closeSoundName);
                }
                break;

            case DoorState.Closing:
                // Vérifie si un joueur bloque
                if (blockIfPlayerInWay && playerInWay)
                {
                    // Ne ferme pas si un joueur est dans le chemin
                    return;
                }

                // Mouvement vers position fermée
                transform.localPosition = Vector3.MoveTowards(
                    transform.localPosition,
                    closedPosition,
                    moveSpeed * Time.deltaTime
                );

                // Arrivée en position fermée ?
                if (Vector3.Distance(transform.localPosition, closedPosition) < 0.01f)
                {
                    transform.localPosition = closedPosition;
                    currentState = DoorState.Closed;
                    stateTimer = closedDuration;
                }
                break;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInWay = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInWay = false;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (crushPlayers && currentState == DoorState.Closing)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                Debug.Log($"Joueur {collision.gameObject.name} écrasé par la porte!");
                FloatingText.Create("ÉCRASÉ!", collision.transform.position + Vector3.up, Color.red);
            }
        }
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

        // Position fermée (rouge)
        Gizmos.color = Color.red;
        Vector3 worldClosed = transform.parent != null ? transform.parent.TransformPoint(closedPosition) : closedPosition;
        Gizmos.DrawWireCube(worldClosed, transform.localScale);

        // Position ouverte (vert)
        Gizmos.color = Color.green;
        Vector3 worldOpen = transform.parent != null ? transform.parent.TransformPoint(openPosition) : openPosition;
        Gizmos.DrawWireCube(worldOpen, transform.localScale);

        // Ligne de trajet
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(worldClosed, worldOpen);
    }

    // Méthode pour forcer l'ouverture (peut être appelée par un bouton, etc.)
    public void ForceOpen()
    {
        currentState = DoorState.Opening;
        PlaySound(openSoundName);
    }

    // Méthode pour forcer la fermeture
    public void ForceClose()
    {
        currentState = DoorState.Closing;
        PlaySound(closeSoundName);
    }
}
