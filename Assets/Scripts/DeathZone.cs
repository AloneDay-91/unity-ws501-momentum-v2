using UnityEngine;
using UnityEngine.EventSystems;

public class DeathZone : MonoBehaviour
{
    public GameObject gameOverPanel; // Assign the Game Over UI Panel in the inspector
    public GameObject firstSelectedButton; // Assign the button to be selected first (e.g., RestartButton)
    public PlayerInput playerInput; // Assign the Player's Input script in the inspector

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger entered by: " + other.name + " with tag: " + other.tag);
        if (other.CompareTag("Player"))
        {
            // Stop the player's timer
            PlayerTimer playerTimer = other.GetComponent<PlayerTimer>();
            if (playerTimer != null)
            {
                playerTimer.StopTimer();
            }

            Debug.Log("Player entered DeathZone. Activating Game Over panel...");
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);

                // Disable player input and pause the game
                if (playerInput != null)
                {
                    playerInput.enabled = false;
                }
                else
                {
                    Debug.LogError("Player Input script is not assigned in the DeathZone script!");
                }
                Time.timeScale = 0f;


                // Set the first button to be selected for controller navigation
                if (firstSelectedButton != null)
                {
                    EventSystem.current.SetSelectedGameObject(firstSelectedButton);
                }
                else
                {
                    Debug.LogError("First Selected Button is not assigned in the DeathZone script!");
                }
            }
            else
            {
                Debug.LogError("Game Over Panel is not assigned in the DeathZone script!");
            }
        }
    }
}
