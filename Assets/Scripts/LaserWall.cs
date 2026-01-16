using UnityEngine;
using System.Collections;

/// <summary>
/// Mur de laser mortel qui avance et élimine les joueurs qui se font rattraper
/// Place ce script sur un GameObject avec un Collider (trigger)
/// </summary>
[RequireComponent(typeof(Collider))]
public class LaserWall : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("Délai avant que le mur ne commence à avancer (en secondes)")]
    public float startDelay = 10f;

    [Tooltip("Démarrer automatiquement au lancement de la scène")]
    public bool autoStart = true;

    [Header("Movement")]
    [Tooltip("Vitesse de déplacement du mur (unités/seconde)")]
    public float moveSpeed = 2f;

    [Tooltip("Direction de déplacement (Vector3.right pour aller vers la droite)")]
    public Vector3 moveDirection = Vector3.right;

    [Tooltip("Accélération progressive (augmente la vitesse au fil du temps)")]
    public float acceleration = 0.1f;

    [Tooltip("Vitesse maximale du mur")]
    public float maxSpeed = 10f;

    [Header("Limits")]
    [Tooltip("Position maximale (le mur s'arrête à cette position X)")]
    public float maxPosition = 1000f;

    [Tooltip("Activer la limite de position")]
    public bool useMaxPosition = false;

    [Header("Player Elimination")]
    [Tooltip("Tag des joueurs à éliminer")]
    public string playerTag = "Player";

    [Tooltip("Délai avant l'élimination après contact (donne du temps au joueur)")]
    public float eliminationDelay = 0.5f;

    [Header("Visual Effects")]
    [Tooltip("Effet de particules à jouer en continu")]
    public ParticleSystem wallParticles;

    [Tooltip("Effet de particules à jouer lors de l'élimination d'un joueur")]
    public GameObject eliminationEffect;

    [Tooltip("Couleur du mur (pour le Renderer)")]
    public Color wallColor = Color.red;

    [Header("Audio")]
    [Tooltip("Son du mur (boucle)")]
    public string wallSoundName = "laserWall";

    [Tooltip("Son d'élimination")]
    public string eliminationSoundName = "playerEliminated";

    [Header("Debug")]
    [Tooltip("Afficher les infos de debug")]
    public bool showDebug = true;

    [Tooltip("Afficher les gizmos")]
    public bool showGizmos = true;

    // État interne
    private bool isMoving = false;
    private float currentSpeed;
    private Collider wallCollider;
    private Renderer wallRenderer;

    // Événement pour notifier l'élimination d'un joueur
    public static System.Action<GameObject> OnPlayerEliminated;

    void Start()
    {
        wallCollider = GetComponent<Collider>();
        wallCollider.isTrigger = true;

        wallRenderer = GetComponent<Renderer>();
        if (wallRenderer != null)
        {
            wallRenderer.material.color = wallColor;
        }

        currentSpeed = moveSpeed;

        // Active les particules si présentes
        if (wallParticles != null)
        {
            wallParticles.Play();
        }

        if (autoStart)
        {
            StartWall();
        }
    }

    /// <summary>
    /// Démarre le mur après le délai défini
    /// </summary>
    public void StartWall()
    {
        StartCoroutine(StartWallCoroutine());
    }

    private IEnumerator StartWallCoroutine()
    {
        if (showDebug)
        {
            Debug.Log($"Mur de laser: Démarrage dans {startDelay} secondes...");
        }

        yield return new WaitForSeconds(startDelay);

        isMoving = true;

        if (showDebug)
        {
            Debug.Log("Mur de laser: ACTIVÉ !");
        }

        // Joue le son du mur
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(wallSoundName))
        {
            AudioManager.Instance.PlaySound(wallSoundName);
        }
    }

    void Update()
    {
        if (!isMoving) return;

        // Déplace le mur
        transform.position += moveDirection.normalized * currentSpeed * Time.deltaTime;

        // Accélération progressive
        if (currentSpeed < maxSpeed)
        {
            currentSpeed += acceleration * Time.deltaTime;
            currentSpeed = Mathf.Min(currentSpeed, maxSpeed);
        }

        // Vérifie la limite de position
        if (useMaxPosition && transform.position.x >= maxPosition)
        {
            isMoving = false;
            if (showDebug)
            {
                Debug.Log("Mur de laser: Position maximale atteinte, arrêt.");
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Vérifie si c'est un joueur
        if (other.CompareTag(playerTag))
        {
            StartCoroutine(EliminatePlayer(other.gameObject));
        }
    }

    void OnTriggerStay(Collider other)
    {
        // Continue de vérifier si le joueur est dans le mur
        if (other.CompareTag(playerTag))
        {
            // Le joueur est toujours dans le mur, on peut ajouter des effets visuels
            // Par exemple, faire clignoter le joueur
        }
    }

    private IEnumerator EliminatePlayer(GameObject player)
    {
        if (showDebug)
        {
            Debug.Log($"Mur de laser: Joueur {player.name} touché ! Élimination dans {eliminationDelay}s...");
        }

        // Donne un peu de temps au joueur (feedback visuel)
        yield return new WaitForSeconds(eliminationDelay);

        // Vérifie si le joueur est toujours en contact
        Collider playerCollider = player.GetComponent<Collider>();
        if (playerCollider != null && wallCollider.bounds.Intersects(playerCollider.bounds))
        {
            EliminatePlayerNow(player);
        }
    }

    private void EliminatePlayerNow(GameObject player)
    {
        if (showDebug)
        {
            Debug.Log($"Mur de laser: Joueur {player.name} ÉLIMINÉ !");
        }

        // Effet de particules
        if (eliminationEffect != null)
        {
            Instantiate(eliminationEffect, player.transform.position, Quaternion.identity);
        }

        // Son d'élimination
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(eliminationSoundName))
        {
            AudioManager.Instance.PlaySoundAtPosition(eliminationSoundName, player.transform.position);
        }

        // Récupère le PlayerInput
        PlayerInput playerInput = player.GetComponent<PlayerInput>();

        // Notifie le ScoreManager de l'élimination (sauvegarde le score)
        if (ScoreManager.Instance != null && playerInput != null)
        {
            ScoreManager.Instance.OnPlayerEliminated(playerInput.playerID);
        }

        // Notifie l'élimination
        OnPlayerEliminated?.Invoke(player);

        // Désactive le joueur
        player.SetActive(false);

        // Affiche le GameOverPanel via GameManager
        if (GameManager.Instance != null)
        {
            // Récupère le nom du joueur
            string playerName = player.name;
            if (playerInput != null)
            {
                playerName = $"Player {playerInput.playerID}";
            }

            // Récupère le score depuis le ScoreManager
            int finalScore = 0;
            if (ScoreManager.Instance != null && playerInput != null)
            {
                finalScore = ScoreManager.Instance.GetPlayerScore(playerInput.playerID);
            }

            // Affiche l'écran de game over
            GameManager.Instance.OnGameFinished(playerName, finalScore);
        }
    }

    /// <summary>
    /// Arrête le mur
    /// </summary>
    public void StopWall()
    {
        isMoving = false;
        if (showDebug)
        {
            Debug.Log("Mur de laser: Arrêté.");
        }
    }

    /// <summary>
    /// Réinitialise le mur à sa position de départ
    /// </summary>
    public void ResetWall(Vector3 startPosition)
    {
        transform.position = startPosition;
        currentSpeed = moveSpeed;
        isMoving = false;

        if (showDebug)
        {
            Debug.Log("Mur de laser: Réinitialisé.");
        }
    }

    /// <summary>
    /// Change la vitesse du mur dynamiquement
    /// </summary>
    public void SetSpeed(float newSpeed)
    {
        currentSpeed = newSpeed;
        if (showDebug)
        {
            Debug.Log($"Mur de laser: Vitesse changée à {newSpeed}");
        }
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // Dessine la direction du mur
        Gizmos.color = isMoving ? Color.red : Color.yellow;
        Gizmos.DrawRay(transform.position, moveDirection.normalized * 5f);

        // Dessine la limite de position
        if (useMaxPosition)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(
                new Vector3(maxPosition, transform.position.y - 50, transform.position.z),
                new Vector3(maxPosition, transform.position.y + 50, transform.position.z)
            );
        }
    }
}
