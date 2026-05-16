using TMPro;
using UnityEngine;

/// <summary>
/// Bandeau discret affichant les contrôles du device courant. À placer sur un objet UI
/// (en bas de l'écran de jeu, ou dans la page « Comment jouer »). Se rafraîchit quand le
/// device change ou qu'un binding clavier est modifié.
/// </summary>
public class ControlHintsBar : MonoBehaviour
{
    [Tooltip("Texte qui affiche la ligne de contrôles")]
    public TMP_Text hintText;

    void Awake()
    {
        if (hintText == null) hintText = GetComponentInChildren<TMP_Text>();
    }

    void OnEnable()
    {
        if (InputDeviceDetector.Instance != null)
        {
            InputDeviceDetector.Instance.OnDeviceChanged += HandleDeviceChanged;
        }
        KeyboardControls.OnChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (InputDeviceDetector.Instance != null)
        {
            InputDeviceDetector.Instance.OnDeviceChanged -= HandleDeviceChanged;
        }
        KeyboardControls.OnChanged -= Refresh;
    }

    private void HandleDeviceChanged(InputDeviceDetector.Device _) => Refresh();

    public void Refresh()
    {
        if (hintText == null) return;
        var device = InputDeviceDetector.Instance != null
            ? InputDeviceDetector.Instance.CurrentDevice
            : InputDeviceDetector.Device.Keyboard;
        hintText.text = ControlScheme.HintLine(device);
    }
}
