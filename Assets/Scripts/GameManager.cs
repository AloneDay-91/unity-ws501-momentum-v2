using UnityEngine;
using UnityEngine.SceneManagement; 
using UnityEngine.EventSystems; // Important pour sélectionner le bouton

public class GameManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    public static GameManager Instance { get; private set; }

    [Header("UI")]
    public GameObject restartButtonUI; 
    
    // --- NOUVELLE VARIABLE ---
    private bool isGameOver = false; // Pour savoir si on doit écouter le bouton

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        Time.timeScale = 1f;
        isGameOver = false; // Le jeu commence
        if (restartButtonUI != null)
        {
            restartButtonUI.SetActive(false);
        }
    }

    // --- NOUVELLE FONCTION ---
    // Update() continue de tourner même si Time.timeScale = 0
    // mais Input.GetButtonDown fonctionne toujours !
    void Update()
    {
        // Si le jeu EST fini...
        if (isGameOver)
        {
            // ...et que le joueur appuie sur P1_B1
            if (Input.GetButtonDown("P1_B1"))
            {
                // ...on relance le jeu
                RestartGame();
            }
        }
    }

    // --- FONCTION MODIFIÉE ---
    public void EndGame()
    {
        isGameOver = true; // On dit au script que le jeu est fini
        Time.timeScale = 0f; // Arrête le temps

        if (restartButtonUI != null)
        {
            restartButtonUI.SetActive(true);
            
            // Ligne bonus : auto-sélectionne le bouton pour un retour visuel
            // (pour qu'il soit surligné)
            EventSystem.current.SetSelectedGameObject(restartButtonUI);
        }
    }

    // (Cette fonction ne change pas)
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}