using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI; // Important pour reconnaître les Boutons

public class AutoSelectFirstButton : MonoBehaviour
{
    // On utilise OnEnable au lieu de Start.
    // Pourquoi ? Parce que si tu fermes le menu et que tu le rouvres,
    // Start ne se relance pas, mais OnEnable OUI.
    void OnEnable()
    {
        // 1. Cherche le premier bouton ACTIF dans les enfants de cet objet
        Button premierBouton = GetComponentInChildren<Button>();

        // 2. Vérifie qu'on a bien trouvé un bouton et que l'EventSystem existe
        if (premierBouton != null && EventSystem.current != null)
        {
            // Vide la sélection précédente pour éviter les bugs
            EventSystem.current.SetSelectedGameObject(null);
            
            // Force la sélection sur le bouton trouvé
            EventSystem.current.SetSelectedGameObject(premierBouton.gameObject);
        }
    }
}