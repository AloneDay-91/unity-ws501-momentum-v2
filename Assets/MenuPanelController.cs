using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class MenuPanelController : MonoBehaviour
{
    [Header("Debug")]
    public bool showDebugLogs = false;

    void OnEnable()
    {
        StartCoroutine(InitializeMenu());
    }

    private IEnumerator InitializeMenu()
    {
        if (showDebugLogs)
        {
            Debug.Log($"[MenuPanel] Initialisation du menu : {gameObject.name}");
        }

        // ÉTAPE 1 : Désélectionner tout immédiatement
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        // ÉTAPE 2 : Récupérer tous les boutons
        Button[] allButtons = GetComponentsInChildren<Button>(true);

        if (showDebugLogs)
        {
            Debug.Log($"[MenuPanel] {allButtons.Length} boutons trouvés");
        }

        // ÉTAPE 3 : Désactiver TOUS les Animators
        foreach (Button btn in allButtons)
        {
            Animator anim = btn.GetComponent<Animator>();
            if (anim != null)
            {
                anim.enabled = false;
            }
        }

        // ÉTAPE 4 : Attendre plusieurs frames pour que Unity se calme
        yield return null;
        yield return null;

        // ÉTAPE 5 : Réinitialiser chaque bouton à Normal
        foreach (Button btn in allButtons)
        {
            ResetButtonToNormal(btn);
        }

        // ÉTAPE 6 : Attendre que les changements soient appliqués
        yield return null;
        yield return null;

        // ÉTAPE 7 : Réactiver les Animators
        foreach (Button btn in allButtons)
        {
            Animator anim = btn.GetComponent<Animator>();
            if (anim != null)
            {
                anim.enabled = true;
            }
        }

        // ÉTAPE 8 : Attendre encore un peu
        yield return null;

        // ÉTAPE 9 : Sélectionner le premier bouton
        SelectFirstButton(allButtons);

        if (showDebugLogs)
        {
            Debug.Log("[MenuPanel] ✓ Initialisation terminée");
        }
    }

    private void ResetButtonToNormal(Button btn)
    {
        if (btn == null) return;

        // Forcer les ColorBlock (méthode la plus fiable)
        ColorBlock colors = btn.colors;
        Image image = btn.targetGraphic as Image;
        
        if (image != null)
        {
            image.color = colors.normalColor;
            
            if (showDebugLogs)
            {
                Debug.Log($"[MenuPanel]   → {btn.name} couleur = Normal");
            }
        }

        // Reset l'Animator si présent
        Animator anim = btn.GetComponent<Animator>();
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            anim.Rebind();
            anim.Play("Normal", 0, 1f);
        }
    }

    private void SelectFirstButton(Button[] buttons)
    {
        if (buttons.Length == 0)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning("[MenuPanel] Aucun bouton à sélectionner");
            }
            return;
        }

        if (EventSystem.current == null)
        {
            Debug.LogError("[MenuPanel] EventSystem introuvable !");
            return;
        }

        // Trouver le premier bouton actif et interactable
        Button firstButton = null;
        foreach (Button btn in buttons)
        {
            if (btn.gameObject.activeInHierarchy && btn.interactable)
            {
                firstButton = btn;
                break;
            }
        }

        if (firstButton != null)
        {
            EventSystem.current.SetSelectedGameObject(firstButton.gameObject);

            if (showDebugLogs)
            {
                Debug.Log($"[MenuPanel] ✓ Bouton sélectionné : {firstButton.name}");
            }
        }
        else if (showDebugLogs)
        {
            Debug.LogWarning("[MenuPanel] Aucun bouton actif/interactable trouvé");
        }
    }

    // === DEBUG ===

    [ContextMenu("Test - Réinitialiser")]
    public void TestReinitialize()
    {
        StartCoroutine(InitializeMenu());
    }

    [ContextMenu("Debug - État des boutons")]
    private void DebugShowState()
    {
        Debug.Log("=== ÉTAT DU MENU ===");
        
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons)
        {
            if (btn.gameObject.activeInHierarchy)
            {
                Image img = btn.targetGraphic as Image;
                string colorInfo = img != null ? $"Couleur: {img.color}" : "Pas d'image";
                
                Animator anim = btn.GetComponent<Animator>();
                string animInfo = anim != null && anim.enabled ? "Anim: ON" : "Anim: OFF";
                
                Debug.Log($"  • {btn.name} | {colorInfo} | {animInfo}");
            }
        }
        
        if (EventSystem.current?.currentSelectedGameObject != null)
        {
            Debug.Log($"✓ Sélectionné: {EventSystem.current.currentSelectedGameObject.name}");
        }
        else
        {
            Debug.Log("✗ Rien de sélectionné");
        }
    }
}