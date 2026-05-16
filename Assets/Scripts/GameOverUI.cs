using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    public void RestartGame()
    {
#if WEB_BUILD
        if (!DevSolo.Active)
        {
            // Rejouer synchronisé : on prévient le serveur et on retourne sur le lobby
            // (« en attente de l'autre joueur »). Quand les deux joueurs ont cliqué,
            // le serveur relance la partie et le lobby recharge "main" automatiquement.
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.SendRematch();
            }
            RematchState.ReturningForRematch = true;
            SceneManager.LoadScene("MainMenu");
            return;
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
