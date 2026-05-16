using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Mur de laser mortel qui avance et élimine les joueurs qui se font rattraper
/// Place ce script sur un GameObject avec un Collider (trigger)
/// </summary>
[RequireComponent(typeof(Collider))]
public class LaserWall : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("Délai avant que le mur ne commence à avancer (en secondes)")]
    public float startDelay = 20f;

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

    // Suivi du contact joueur↔mur : temps de contact continu accumulé par joueur.
    // Un joueur est éliminé dès qu'il reste en contact eliminationDelay secondes.
    private readonly Dictionary<GameObject, float> contactTimers = new Dictionary<GameObject, float>();
    private readonly List<GameObject> _contactKeysBuffer = new List<GameObject>();

    // Événement pour notifier l'élimination d'un joueur
    public static System.Action<GameObject> OnPlayerEliminated;

    void Start()
    {
        wallCollider = GetComponent<Collider>();
        if (wallCollider != null)
        {
            wallCollider.isTrigger = true;
        }

        wallRenderer = GetComponent<Renderer>();
        if (wallRenderer != null)
        {
            wallRenderer.material.color = wallColor;
        }

        currentSpeed = moveSpeed;

        // Désactive les particules au début (elles s'activeront au démarrage)
        if (wallParticles != null)
        {
            wallParticles.Stop();
        }

        // Rend le mur invisible au début
        SetWallVisibility(false);

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

        // Hold until the match is actually running. The Update() check below pauses
        // movement once isMoving is true, but visibility/sound trigger from this coroutine,
        // so we also gate the whole startup behind gameInProgress.
        while (GameManager.Instance != null && !GameManager.Instance.gameInProgress)
        {
            yield return null;
        }

        yield return new WaitForSeconds(startDelay);

        isMoving = true;

        // Rend le mur visible
        SetWallVisibility(true);

        // Active les particules
        if (wallParticles != null)
        {
            wallParticles.Play();
        }

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

        // Don't advance until the server-driven countdown has actually started the match.
        // Without this, in WEB_BUILD the wall starts moving while we're still on the
        // "En attente de l'autre joueur" / "3,2,1" overlay → unfair.
        if (GameManager.Instance != null && !GameManager.Instance.gameInProgress) return;

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

        // Élimination par contrôle continu du contact (et non via une coroutine à
        // instant unique) — robuste aux entrées/sorties répétées et au collage au bord.
        UpdatePlayerContacts();
    }

    void OnTriggerEnter(Collider other)
    {
        // Enregistre le joueur pour le suivi de contact. L'élimination réelle est faite
        // par UpdatePlayerContacts (contrôle continu chaque frame).
        if (other.CompareTag(playerTag))
        {
            if (!contactTimers.ContainsKey(other.gameObject))
            {
                contactTimers[other.gameObject] = 0f;
            }
            if (showDebug)
            {
                Debug.Log($"Mur de laser: Joueur {other.name} en contact — suivi démarré");
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Le joueur a quitté le mur — on arrête de le suivre.
        if (other.CompareTag(playerTag))
        {
            contactTimers.Remove(other.gameObject);
        }
    }

    /// <summary>
    /// Contrôle continu : accumule le temps de contact de chaque joueur suivi et
    /// l'élimine après eliminationDelay secondes de contact ininterrompu. Sortir du
    /// mur remet le délai de grâce à zéro.
    /// </summary>
    private void UpdatePlayerContacts()
    {
        if (contactTimers.Count == 0) return;

        _contactKeysBuffer.Clear();
        _contactKeysBuffer.AddRange(contactTimers.Keys);

        GameObject toEliminate = null;

        foreach (var player in _contactKeysBuffer)
        {
            if (player == null || !player.activeInHierarchy)
            {
                contactTimers.Remove(player);
                continue;
            }

            Collider playerCollider = player.GetComponent<Collider>();
            bool inContact = playerCollider != null
                && wallCollider != null
                && wallCollider.bounds.Intersects(playerCollider.bounds);

            if (inContact)
            {
                contactTimers[player] += Time.deltaTime;
                if (contactTimers[player] >= eliminationDelay)
                {
                    toEliminate = player;
                }
            }
            else
            {
                // Sorti du mur : le délai de grâce repart de zéro.
                contactTimers[player] = 0f;
            }
        }

        if (toEliminate != null)
        {
            contactTimers.Remove(toEliminate);
            EliminatePlayerNow(toEliminate);
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
        int playerID = playerInput != null ? playerInput.playerID : 1;

        // Récupère le score depuis le ScoreManager AVANT de notifier l'élimination
        int finalScore = 0;
        if (ScoreManager.Instance != null)
        {
            finalScore = ScoreManager.Instance.CalculateScore(playerID);
        }

        // Notifie l'élimination (événement statique)
        OnPlayerEliminated?.Invoke(player);

        // Désactive le joueur
        player.SetActive(false);

        // Notifie le GameManager de l'élimination
        // GameManager va ensuite notifier ScoreManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerEliminated(playerID, finalScore);
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
        contactTimers.Clear();

        // Rend le mur invisible à nouveau
        SetWallVisibility(false);

        // Arrête les particules
        if (wallParticles != null)
        {
            wallParticles.Stop();
        }

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

    /// <summary>
    /// Active ou désactive la visibilité du mur
    /// </summary>
    private void SetWallVisibility(bool visible)
    {
        if (wallRenderer != null)
        {
            wallRenderer.enabled = visible;
        }

        // Active/désactive aussi les renderers des enfants (si le mur a des composants visuels)
        Renderer[] childRenderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in childRenderers)
        {
            renderer.enabled = visible;
        }

        // Active/désactive le collider pour éviter les collisions avec un mur invisible
        if (wallCollider != null)
        {
            wallCollider.enabled = visible;
        }

        if (showDebug)
        {
            Debug.Log($"Mur de laser: Visibilité = {visible}");
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
