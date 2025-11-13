using UnityEngine;
using System.Collections;

public class GameCycleManager : MonoBehaviour
{
    // --- Singleton Pattern (pour un accès facile) ---
    public static GameCycleManager Instance { get; private set; }

    [Header("Configuration du Cycle")]
    public float phaseDuration = 20f; // 20 secondes par phase
    
    // État actuel
    public bool IsDay { get; private set; }
    
    // Minuteur
    private float cycleTimer;

    // --- Événements (pour notifier les autres scripts) ---
    // D'autres scripts pourront s'abonner à ces "signaux"
    public static event System.Action OnDayStart;
    public static event System.Action OnNightStart;

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
        // On commence par la phase Jour
        StartDayPhase();
    }

    void Update()
    {
        // On fait avancer le minuteur
        cycleTimer -= Time.deltaTime;

        // Si le minuteur atteint zéro
        if (cycleTimer <= 0)
        {
            // On change de phase
            if (IsDay)
            {
                StartNightPhase();
            }
            else
            {
                StartDayPhase();
            }
        }
    }

    void StartDayPhase()
    {
        Debug.Log("PHASE JOUR (Collectez !)");
        IsDay = true;
        cycleTimer = phaseDuration;
        
        // On envoie le signal "Jour"
        if (OnDayStart != null)
        {
            OnDayStart();
        }
    }

    void StartNightPhase()
    {
        Debug.Log("PHASE NUIT (Utilisez la lumière !)");
        IsDay = false;
        cycleTimer = phaseDuration;
        
        // On envoie le signal "Nuit"
        if (OnNightStart != null)
        {
            OnNightStart();
        }
    }
}