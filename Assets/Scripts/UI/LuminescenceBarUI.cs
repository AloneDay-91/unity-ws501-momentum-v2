using UnityEngine;
using UnityEngine.UI; // Important pour le Slider

[RequireComponent(typeof(Slider))]
public class LuminescenceBarUI : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Quel joueur cette barre doit-elle suivre ? (1 ou 2)")]
    public int playerIDToTrack = 1;

    private Slider luminescenceSlider;
    private PlayerStats targetPlayerStats;

    void Start()
    {
        luminescenceSlider = GetComponent<Slider>();
        
        // Trouve le bon script PlayerStats à suivre
        PlayerStats[] allPlayers = FindObjectsOfType<PlayerStats>();
        foreach (PlayerStats player in allPlayers)
        {
            if (player.GetComponent<PlayerInput>().playerID == playerIDToTrack)
            {
                targetPlayerStats = player;
                break;
            }
        }
    }

    void Update()
    {
        // Si on a bien trouvé le joueur
        if (targetPlayerStats != null)
        {
            // Met à jour la valeur du Slider
            float fillValue = targetPlayerStats.currentLuminescence / targetPlayerStats.maxLuminescence;
            luminescenceSlider.value = fillValue;
        }
    }
}