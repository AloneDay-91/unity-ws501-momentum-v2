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

    // Élimination par franchissement de plan : le mur est une fine tranche (scale X≈0),
    // donc les triggers/overlaps de collision sont peu fiables. On considère plutôt qu'un
    // joueur est « rattrapé » dès que le plan du mur l'a dépassé. behindTimers accumule
    // le temps passé derrière le mur, par joueur.
    private readonly Dictionary<GameObject, float> behindTimers = new Dictionary<GameObject, float>();
    private readonly List<GameObject> _playersBuffer = new List<GameObject>();

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

        // Élimination : un joueur que le plan du mur a dépassé est « rattrapé ».
        CheckPlayersCaught();
    }

    /// <summary>
    /// Élimination par franchissement de plan. Le mur est une fine tranche : se fier aux
    /// triggers de collision est peu fiable (le joueur traverse entre deux frames, et
    /// l'ancien test à instant unique ratait selon la vitesse relative et le sens de
    /// passage). On considère qu'un joueur est rattrapé dès que le plan du mur l'a
    /// dépassé, et on l'élimine après eliminationDelay secondes passées derrière le mur
    /// — repasser devant remet le délai de grâce à zéro.
    /// </summary>
    private void CheckPlayersCaught()
    {
        // Init paresseux : on récupère les joueurs une fois le mur actif. Le startDelay
        // garantit qu'ils existent tous à ce moment (countdown fini, joueur distant spawné).
        if (behindTimers.Count == 0)
        {
            foreach (var go in GameObject.FindGameObjectsWithTag(playerTag))
            {
                behindTimers[go] = 0f;
            }
            if (behindTimers.Count == 0) return;
        }

        Vector3 dir = moveDirection.normalized;
        if (dir == Vector3.zero) return;

        _playersBuffer.Clear();
        _playersBuffer.AddRange(behindTimers.Keys);

        GameObject toEliminate = null;

        foreach (var player in _playersBuffer)
        {
            if (player == null || !player.activeInHierarchy)
            {
                behindTimers.Remove(player);
                continue;
            }

            // Projection du vecteur mur→joueur sur la direction d'avancée du mur.
            // along > 0 : le joueur est encore devant le mur. along <= 0 : le mur l'a dépassé.
            float along = Vector3.Dot(player.transform.position - transform.position, dir);

            if (along <= 0f)
            {
                behindTimers[player] += Time.deltaTime;
                if (behindTimers[player] >= eliminationDelay)
                {
                    toEliminate = player;
                }
            }
            else
            {
                // Le joueur est repassé devant le mur : le délai de grâce repart de zéro.
                behindTimers[player] = 0f;
            }
        }

        if (toEliminate != null)
        {
            behindTimers.Remove(toEliminate);
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
        behindTimers.Clear();

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
