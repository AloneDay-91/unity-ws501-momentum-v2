using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    public void RestartGame()
    {
        // Unpause the game
        Time.timeScale = 1f;
        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        // Unpause the game
        Time.timeScale = 1f;
        // Load the MainMenu scene
        SceneManager.LoadScene("MainMenu");
    }
}
