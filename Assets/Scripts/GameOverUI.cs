using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    public void RestartGame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
        else
        {
            // Fallback if GameManager is missing
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public void QuitGame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.QuitToMenu();
        }
        else
        {
            // Fallback
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }
    }
}
