using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    public void RestartGame()
    {
#if WEB_BUILD
        // En multijoueur web, « Rejouer » est synchronisé : on passe par RematchController
        // au lieu de recharger la scène localement (sinon l'autre joueur ne suit pas).
        var rematch = FindObjectOfType<RematchController>();
        if (rematch != null)
        {
            rematch.RequestRematch();
            return;
        }
        Debug.LogWarning("[GameOverUI] RematchController introuvable — fallback reload local");
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
