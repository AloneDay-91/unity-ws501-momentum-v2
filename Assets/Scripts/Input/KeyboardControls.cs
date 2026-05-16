using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Touches clavier du joueur local web. Un KeyCode fixe par action.
/// </summary>
public static class KeyboardControls
{
    public enum Action { Left, Right, Jump, Slide, Light }

    private static readonly Dictionary<Action, KeyCode> Keys = new Dictionary<Action, KeyCode>
    {
        { Action.Left,  KeyCode.Q },
        { Action.Right, KeyCode.D },
        { Action.Jump,  KeyCode.Space },
        { Action.Slide, KeyCode.LeftControl },
        { Action.Light, KeyCode.F },
    };

    public static KeyCode Get(Action action) => Keys[action];
}
