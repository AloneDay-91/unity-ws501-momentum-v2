using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    public void RestartGame()
    {
#if WEB_BUILD
        // En multijoueur web, « Rejouer » est synchronisé via RematchController.
        // En mode solo dev (hors-ligne) il n'y a pas de réseau : on saute le rematch
        // et on recharge la scène en local.
        if (!DevSolo.Active)
        {
            var rematch = FindObjectOfType<RematchController>();
            if (rematch != null)
            {
                rematch.RequestRematch();
                return;
            }
            Debug.LogError("[GameOverUI] RematchController introuvable — fallback reload local");
        }
#endif
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
