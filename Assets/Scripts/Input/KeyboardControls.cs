using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bindings clavier du joueur local web. Un KeyCode par action, persisté en PlayerPrefs.
/// En web il n'y a qu'un seul joueur local par navigateur : un seul jeu de touches,
/// personnalisable par la personne qui joue sur cette machine.
/// </summary>
public static class KeyboardControls
{
    public enum Action { Left, Right, Jump, Slide, Light }

    private const string Prefix = "kb_";

    private static readonly Dictionary<Action, KeyCode> Defaults = new Dictionary<Action, KeyCode>
    {
        { Action.Left,  KeyCode.LeftArrow },
        { Action.Right, KeyCode.RightArrow },
        { Action.Jump,  KeyCode.Space },
        { Action.Slide, KeyCode.LeftShift },
        { Action.Light, KeyCode.E },
    };

    /// <summary>
    /// Émis après chaque Set / ResetToDefaults — l'UI et le bandeau s'y abonnent.
    /// Qualifié `System.Action` car l'enum imbriquée `Action` masque l'import `using System;`.
    /// </summary>
    public static System.Action OnChanged;

    public static IEnumerable<Action> AllActions => Defaults.Keys;

    public static KeyCode GetDefault(Action action) => Defaults[action];

    public static KeyCode Get(Action action)
    {
        string raw = PlayerPrefs.GetString(Prefix + action, "");
        if (!string.IsNullOrEmpty(raw) && Enum.TryParse(raw, out KeyCode kc))
        {
            return kc;
        }
        return Defaults[action];
    }

    public static void Set(Action action, KeyCode key)
    {
        PlayerPrefs.SetString(Prefix + action, key.ToString());
        PlayerPrefs.Save();
        OnChanged?.Invoke();
    }

    public static void ResetToDefaults()
    {
        foreach (var action in Defaults.Keys)
        {
            PlayerPrefs.DeleteKey(Prefix + action);
        }
        PlayerPrefs.Save();
        OnChanged?.Invoke();
    }
}
