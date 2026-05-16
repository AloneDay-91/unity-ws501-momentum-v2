using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Touches clavier du joueur local web. Un KeyCode fixe par action.
/// </summary>
public static class KeyboardControls
{
    public enum Action { Left, Right, Jump, Slide, Light }

    // KeyCode est positionnel (clavier QWERTY de référence) : KeyCode.A correspond à la
    // touche physique étiquetée « Q » sur un clavier AZERTY. Left utilise donc KeyCode.A
    // pour que la touche « Q » de l'AZERTY déplace à gauche.
    private static readonly Dictionary<Action, KeyCode> Keys = new Dictionary<Action, KeyCode>
    {
        { Action.Left,  KeyCode.A },
        { Action.Right, KeyCode.D },
        { Action.Jump,  KeyCode.Space },
        { Action.Slide, KeyCode.LeftControl },
        { Action.Light, KeyCode.F },
    };

    public static KeyCode Get(Action action) => Keys[action];
}
