using UnityEngine;

/// <summary>
/// Ligne d'arrivée qui détecte quand un joueur termine le parcours
/// Place ce script sur un GameObject avec un Collider (trigger) à la position de fin
/// </summary>
[RequireComponent(typeof(Collider))]
public class FinishLine : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Tag des joueurs")]
    public string playerTag = "Player";

    [Header("Visual Effects")]
    [Tooltip("Effet de particules à jouer quand un joueur termine")]
    public GameObject finishEffect;

    [Header("Audio")]
    [Tooltip("Son de victoire")]
    public string victorySoundName = "victory";

    [Header("Debug")]
    public bool showDebug = true;
    public bool showGizmos = true;

    private Collider finishCollider;

    // Événement pour notifier quand un joueur termine
    public static System.Action<GameObject> OnPlayerFinished;

    void Start()
    {
        finishCollider = GetComponent<Collider>();
        finishCollider.isTrigger = true;

        if (showDebug)
        {
            Debug.Log($"FinishLine: Ligne d'arrivée activée à position X={transform.position.x}");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Vérifie si c'est un joueur
        if (other.CompareTag(playerTag))
        {
            OnPlayerCrossedFinishLine(other.gameObject);
        }
    }

    private void OnPlayerCrossedFinishLine(GameObject player)
    {
        if (showDebug)
        {
            Debug.Log($"FinishLine: Joueur {player.name} a franchi la ligne d'arrivée!");
        }

        // Effet de particules
        if (finishEffect != null)
        {
            Instantiate(finishEffect, player.transform.position, Quaternion.identity);
        }

        // Son de victoire
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(victorySoundName))
        {
            AudioManager.Instance.PlaySoundAtPosition(victorySoundName, player.transform.position);
        }

        // Récupère le PlayerInput
        PlayerInput playerInput = player.GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            Debug.LogWarning("FinishLine: PlayerInput non trouvé sur le joueur!");
            return;
        }

        // Notifie le ScoreManager
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnPlayerFinished(playerInput.playerID);
        }

        // Notifie l'événement
        OnPlayerFinished?.Invoke(player);

        // Calcule le temps et le score
        if (GameManager.Instance != null)
        {
            // Le joueur a gagné! Affiche l'écran de victoire
            int finalScore = ScoreManager.Instance != null ?
                ScoreManager.Instance.GetPlayerScore(playerInput.playerID) : 0;

            string playerName = $"Player {playerInput.playerID}";
            GameManager.Instance.OnPlayerWin(playerName, finalScore);
        }
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // Dessine la ligne d'arrivée
        Gizmos.color = Color.green;
        Gizmos.DrawLine(
            transform.position + Vector3.up * 10f,
            transform.position + Vector3.down * 10f
        );

        // Dessine une zone autour
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.DrawCube(transform.position, col.bounds.size);
        }
    }
}
