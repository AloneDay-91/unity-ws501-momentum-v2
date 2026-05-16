#if WEB_BUILD
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gère le « Rejouer » synchronisé en multijoueur web. Le bouton Rejouer du GameOverPanel
/// appelle RequestRematch() : on envoie le message au serveur et on passe en attente.
/// Quand les deux joueurs ont demandé le rematch, le serveur repasse status="loading"
/// → GameSessionManager.OnGameStarted fire → on recharge la scène de jeu.
/// Si l'autre joueur quitte pendant l'attente, on affiche un message et seul « Quitter » reste.
/// </summary>
public class RematchController : MonoBehaviour
{
    [Header("Nom de la scène de jeu à recharger")]
    public string gameSceneName = "main";

    [Header("UI — états du rematch")]
    [Tooltip("Bouton Rejouer (caché une fois cliqué)")]
    public GameObject rematchButton;
    [Tooltip("Message « En attente de l'autre joueur… »")]
    public GameObject waitingMessage;
    [Tooltip("Message « L'autre joueur a quitté la partie »")]
    public GameObject opponentLeftMessage;

    private bool _waitingForRematch = false;
    private bool _reloadTriggered = false;

    void OnEnable()
    {
        GameSessionManager.OnGameStarted += HandleGameStarted;
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnPlayerRemoved += HandlePlayerRemoved;
        }
    }

    void OnDisable()
    {
        GameSessionManager.OnGameStarted -= HandleGameStarted;
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnPlayerRemoved -= HandlePlayerRemoved;
        }
    }

    /// <summary>Appelé par le bouton « Rejouer » du GameOverPanel.</summary>
    public void RequestRematch()
    {
        if (_waitingForRematch) return;
        _waitingForRematch = true;

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.SendRematch();
        }

        if (rematchButton != null) rematchButton.SetActive(false);
        if (waitingMessage != null) waitingMessage.SetActive(true);
        if (opponentLeftMessage != null) opponentLeftMessage.SetActive(false);
    }

    // Le serveur a repassé status="loading" (les deux joueurs veulent rejouer) →
    // GameSessionManager fire OnGameStarted. On recharge la scène de jeu.
    private void HandleGameStarted()
    {
        if (_reloadTriggered) return;
        _reloadTriggered = true;
        SceneManager.LoadScene(gameSceneName);
    }

    // Un joueur a quitté. Si on attendait le rematch, c'est une annulation : on prévient.
    private void HandlePlayerRemoved(string _)
    {
        if (!_waitingForRematch || _reloadTriggered) return;

        if (waitingMessage != null) waitingMessage.SetActive(false);
        if (opponentLeftMessage != null) opponentLeftMessage.SetActive(true);
        if (rematchButton != null) rematchButton.SetActive(false);
    }
}
#endif
