/// <summary>
/// Source des libellés de contrôles affichés (bandeau en jeu + page « Comment jouer »).
/// </summary>
public static class ControlScheme
{
    /// <summary>Ligne de rappel des contrôles pour le device donné.</summary>
    public static string HintLine(InputDeviceDetector.Device device)
    {
        if (device == InputDeviceDetector.Device.Gamepad)
        {
            return "Stick  Déplacer     A  Sauter     B  Glisser     X  Lumière";
        }

        // Clavier AZERTY : les touches physiques utilisées sont Q, D, Espace, Ctrl, F.
        return "Q/D  Déplacer     Espace  Sauter     Ctrl  Glisser     F  Lumière";
    }
}
