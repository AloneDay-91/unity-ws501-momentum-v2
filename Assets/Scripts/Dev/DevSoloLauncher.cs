#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Éditeur uniquement. Active le mode solo dev (DevSolo.Active) de deux façons :
///  - F9 depuis une scène ≠ "main" → passe le flag et charge "main".
///  - Au lancement, si la scène de boot est déjà "main" (le dev a ouvert main.unity
///    et pressé Play) → solo automatique.
/// N'existe jamais dans une build livrée (#if UNITY_EDITOR).
/// </summary>
public class DevSoloLauncher : MonoBehaviour
{
    private const string GameSceneName = "main";

    // Détection « boot direct sur main ». AfterSceneLoad s'exécute après le chargement
    // de la scène initiale mais AVANT les Start(), donc avant que NetworkManager,
    // GameSessionManager et GameManager ne lisent DevSolo.Active.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void DetectBootScene()
    {
        if (SceneManager.GetActiveScene().name == GameSceneName)
        {
            DevSolo.Active = true;
            Debug.Log("[DevSoloLauncher] Boot direct sur 'main' → mode solo dev activé");
        }
    }

    // Crée le composant persistant qui écoute F9.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoBootstrap()
    {
        if (FindObjectOfType<DevSoloLauncher>() != null) return;
        var go = new GameObject("DevSoloLauncher (editor)");
        go.AddComponent<DevSoloLauncher>();
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9)
            && SceneManager.GetActiveScene().name != GameSceneName)
        {
            DevSolo.Active = true;
            Debug.Log("[DevSoloLauncher] F9 → chargement de 'main' en mode solo dev");
            SceneManager.LoadScene(GameSceneName);
        }
    }
}
#endif
