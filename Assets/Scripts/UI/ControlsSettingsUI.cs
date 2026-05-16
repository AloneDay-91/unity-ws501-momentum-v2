using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Écran de personnalisation des touches clavier. Une ligne par action : libellé de la
/// touche actuelle + bouton « Réassigner » qui capture la prochaine touche pressée.
/// </summary>
public class ControlsSettingsUI : MonoBehaviour
{
    [System.Serializable]
    public class ActionRow
    {
        [Tooltip("L'action concernée")]
        public KeyboardControls.Action action;
        [Tooltip("Texte affichant la touche actuelle")]
        public TMP_Text keyLabel;
        [Tooltip("Bouton qui lance la capture pour cette action")]
        public Button rebindButton;
    }

    [Header("Lignes d'actions")]
    public List<ActionRow> rows = new List<ActionRow>();

    [Header("Boutons globaux")]
    [Tooltip("Bouton qui restaure les touches par défaut")]
    public Button resetButton;
    [Tooltip("Bouton de retour (ferme l'écran)")]
    public Button backButton;

    [Header("Capture")]
    [Tooltip("Texte affiché pendant la capture d'une touche")]
    public string capturingLabel = "Appuie sur une touche…";

    private static readonly KeyCode[] AllKeyCodes = (KeyCode[])System.Enum.GetValues(typeof(KeyCode));

    private ActionRow capturingRow = null;

    void Start()
    {
        foreach (var row in rows)
        {
            var captured = row; // capture locale pour la lambda
            if (captured.rebindButton != null)
            {
                captured.rebindButton.onClick.AddListener(() => BeginCapture(captured));
            }
        }
        if (resetButton != null) resetButton.onClick.AddListener(OnResetClicked);
        if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
    }

    void OnEnable()
    {
        capturingRow = null;
        RefreshAll();
    }

    void Update()
    {
        if (capturingRow == null) return;

        // Échap annule la capture sans rien changer.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            capturingRow = null;
            RefreshAll();
            return;
        }

        foreach (KeyCode key in AllKeyCodes)
        {
            if (!Input.GetKeyDown(key)) continue;
            if (!IsAssignableKey(key)) continue;

            KeyboardControls.Set(capturingRow.action, key);
            capturingRow = null;
            RefreshAll();
            return;
        }
    }

    /// <summary>Refuse souris et boutons de manette — on ne rebinde que le clavier.</summary>
    private static bool IsAssignableKey(KeyCode key)
    {
        if (key >= KeyCode.Mouse0 && key <= KeyCode.Mouse6) return false;
        if (key >= KeyCode.JoystickButton0 && key <= KeyCode.JoystickButton19) return false;
        if (key == KeyCode.None || key == KeyCode.Escape) return false;
        return true;
    }

    private void BeginCapture(ActionRow row)
    {
        capturingRow = row;
        if (row.keyLabel != null) row.keyLabel.text = capturingLabel;
    }

    private void RefreshAll()
    {
        foreach (var row in rows)
        {
            if (row.keyLabel != null)
            {
                row.keyLabel.text = ControlScheme.PrettyKey(KeyboardControls.Get(row.action));
            }
        }
    }

    private void OnResetClicked()
    {
        KeyboardControls.ResetToDefaults();
        capturingRow = null;
        RefreshAll();
    }

    private void OnBackClicked()
    {
        capturingRow = null;
        if (MenuPageManager.Instance != null)
        {
            MenuPageManager.Instance.ShowHomePage();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
