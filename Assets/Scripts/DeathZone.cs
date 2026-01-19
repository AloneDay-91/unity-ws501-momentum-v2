using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Zone de mort (chute dans le vide, etc.)
/// Élimine le joueur qui entre en contact
/// </summary>
public class DeathZone : MonoBehaviour
{
    [Header("Legacy (optionnel - utilisé si GameManager n'existe pas)")]
    public GameObject gameOverPanel;
    public GameObject firstSelectedButton;

    [Header("Debug")]
    public bool showDebug = true;

    private void OnTriggerEnter(Collider other)
    {
        if (showDebug)
        {
            Debug.Log($"DeathZone: Trigger entered by {other.name} with tag {other.tag}");
        }

        if (other.CompareTag("Player"))
        {
            EliminatePlayer(other.gameObject);
        }
    }

    private void EliminatePlayer(GameObject player)
    {
        // Récupère le PlayerInput pour avoir l'ID du joueur
        PlayerInput playerInput = player.GetComponent<PlayerInput>();
        int playerID = playerInput != null ? playerInput.playerID : 1;

        if (showDebug)
        {
            Debug.Log($"DeathZone: Joueur {playerID} éliminé (chute dans le vide)");
        }

        // Stop the player's timer
        PlayerTimer playerTimer = player.GetComponent<PlayerTimer>();
        if (playerTimer != null)
        {
            playerTimer.StopTimer();
        }

        // Récupère le score depuis le ScoreManager
        int finalScore = 0;
        if (ScoreManager.Instance != null)
        {
            finalScore = ScoreManager.Instance.CalculateScore(playerID);
        }

        // Désactive le joueur
        player.SetActive(false);

        // Utilise le nouveau système GameManager
        if (GameManager.Instance != null)
        {
            // Notifie le GameManager (qui gère l'overlay individuel + GameOverPanel final)
            GameManager.Instance.OnPlayerEliminated(playerID, finalScore);
        }
        else
        {
            // Fallback: ancien système (si GameManager n'existe pas)
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
                Time.timeScale = 0f;

                if (firstSelectedButton != null)
                {
                    EventSystem.current.SetSelectedGameObject(firstSelectedButton);
                }
            }
        }
    }
}
