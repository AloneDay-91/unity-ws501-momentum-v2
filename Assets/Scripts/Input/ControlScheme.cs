/// <summary>
/// Source unique des libellés de contrôles affichés (bandeau en jeu + page « Comment jouer »).
/// Les touches clavier reflètent les bindings personnalisés ; la manette a des libellés fixes.
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

        return $"{Label(KeyboardControls.Action.Left)}/{Label(KeyboardControls.Action.Right)}  Déplacer     "
             + $"{Label(KeyboardControls.Action.Jump)}  Sauter     "
             + $"{Label(KeyboardControls.Action.Slide)}  Glisser     "
             + $"{Label(KeyboardControls.Action.Light)}  Lumière";
    }

    /// <summary>Libellé lisible d'une touche clavier rebindable.</summary>
    public static string Label(KeyboardControls.Action action)
    {
        return PrettyKey(KeyboardControls.Get(action));
    }

    /// <summary>Rend un KeyCode lisible (ex : "LeftArrow" → "←", "LeftShift" → "Maj").</summary>
    public static string PrettyKey(UnityEngine.KeyCode key)
    {
        switch (key)
        {
            case UnityEngine.KeyCode.LeftArrow:  return "←";
            case UnityEngine.KeyCode.RightArrow: return "→";
            case UnityEngine.KeyCode.UpArrow:    return "↑";
            case UnityEngine.KeyCode.DownArrow:  return "↓";
            case UnityEngine.KeyCode.Space:      return "Espace";
            case UnityEngine.KeyCode.LeftShift:  return "Maj G";
            case UnityEngine.KeyCode.RightShift: return "Maj D";
            case UnityEngine.KeyCode.LeftControl:  return "Ctrl G";
            case UnityEngine.KeyCode.RightControl: return "Ctrl D";
            case UnityEngine.KeyCode.Return:     return "Entrée";
            default: return key.ToString();
        }
    }
}
