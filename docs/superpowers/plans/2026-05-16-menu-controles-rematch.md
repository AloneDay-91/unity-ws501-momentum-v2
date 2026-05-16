# Menu, contrôles & rejouer — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Corriger la perte de session lors de la navigation menu, ajouter la détection clavier/manette avec affichage des touches, permettre la personnalisation des touches clavier, et synchroniser le bouton « Rejouer » entre les deux joueurs.

**Architecture:** 4 fonctionnalités indépendantes, toutes ciblant le mode WEB_BUILD. Côté Unity : un fix de navigation, une couche d'input clavier custom (l'ancien Input Manager ne permet pas le rebinding runtime), un détecteur de device, des composants UI. Côté serveur Colyseus : un message `rematch` qui remet l'état de la room à `loading` quand les deux joueurs sont d'accord, ce qui réutilise le handshake de démarrage existant. Côté Next.js : rendre `/api/game/end` idempotent.

**Tech Stack:** Unity (C#, ancien Input Manager, Colyseus SDK), Node.js + Colyseus + TypeScript + vitest (serveur), Next.js + Prisma (site).

**Repos :**
- `momentum-game-v2` — `/Users/elouan/Desktop/WS501D/momentum-game-v2`
- `momentum-server` — `/Users/elouan/buts4/www/html/SAE501/momentum-server`
- `momentum` — `/Users/elouan/buts4/www/html/SAE501/momentum`

**Note Unity :** ce projet n'a pas de framework de tests Unity. Les étapes de vérification Unity sont des contrôles manuels en Play Mode / build WebGL. Le code sous `#if WEB_BUILD` ne compile que si le symbole `WEB_BUILD` est présent dans Player Settings → Scripting Define Symbols (ou via une vraie build WebGL). Le serveur, lui, a vitest configuré — les tâches serveur sont en TDD.

---

## Phase 1 — Feature 1 : persistance de la session

### Task 1 : Ne pas réinitialiser la session sur « Retour » en WEB_BUILD

**Files:**
- Modify: `momentum-game-v2/Assets/Scripts/LobbyPageUI.cs:219-237`

- [ ] **Step 1 : Remplacer le corps de `OnBackButtonClicked()`**

Remplacer entièrement la méthode `OnBackButtonClicked()` (lignes 216-237) par :

```csharp
    /// <summary>
    /// Retour au menu. En WEB_BUILD la session vient de l'URL et la connexion Colyseus
    /// est persistante — on ne la détruit PAS, on navigue juste vers la home pour que le
    /// joueur puisse consulter « Comment jouer » puis revenir au lobby sans perdre la partie.
    /// </summary>
    public void OnBackButtonClicked()
    {
        if (MenuPageManager.Instance == null) return;

#if WEB_BUILD
        // Pas de ResetSession() : sessionId, room Colyseus, pseudos et bothPlayersReady
        // doivent survivre à un aller-retour dans le menu.
        MenuPageManager.Instance.ShowHomePage();
#else
        // En arcade, le « Retour » annule réellement la session en cours.
        if (GameSessionManager.Instance != null)
        {
            GameSessionManager.Instance.ResetSession();
        }
        MenuPageManager.Instance.ShowQRCodePage();
#endif
    }
```

- [ ] **Step 2 : Vérification manuelle (build WebGL ou éditeur avec `WEB_BUILD` défini)**

Lancer le jeu en mode web. Flux à tester :
1. Page d'accueil → cliquer « Démarrer » → on arrive sur le lobby.
2. Cliquer « Retour » → on revient à l'accueil.
3. Cliquer « Comment jouer » → la page tutoriel s'affiche → « Retour ».
4. Cliquer « Démarrer » à nouveau.

Attendu : le lobby réaffiche les mêmes pseudos et le même état « prêt » qu'à l'étape 1. Dans la console Unity, **aucun** log `GameSession: Création de session via …` ne doit apparaître au second « Démarrer » (on doit voir `WEB_BUILD: CreateGameSession skipped — reusing inherited sessionId`).

- [ ] **Step 3 : Commit**

```bash
cd /Users/elouan/Desktop/WS501D/momentum-game-v2
git add Assets/Scripts/LobbyPageUI.cs
git commit -m "fix(menu): keep web session alive when navigating back from lobby"
```

---

## Phase 2 — Feature 3 (modèle) : couche de touches clavier

### Task 2 : `KeyboardControls` — bindings clavier persistants

**Files:**
- Create: `momentum-game-v2/Assets/Scripts/Input/KeyboardControls.cs`

- [ ] **Step 1 : Créer le fichier**

```csharp
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
```

- [ ] **Step 2 : Vérifier la compilation**

Revenir dans l'éditeur Unity, laisser recompiler. Attendu : aucune erreur dans la console.

- [ ] **Step 3 : Commit**

```bash
cd /Users/elouan/Desktop/WS501D/momentum-game-v2
git add Assets/Scripts/Input/KeyboardControls.cs
git commit -m "feat(input): add persistent rebindable keyboard controls layer"
```

---

## Phase 3 — Feature 2 : détection de device + affichage des touches

### Task 3 : `InputDeviceDetector` — clavier vs manette

**Files:**
- Create: `momentum-game-v2/Assets/Scripts/Input/InputDeviceDetector.cs`

- [ ] **Step 1 : Créer le fichier**

```csharp
using UnityEngine;

/// <summary>
/// Détecte le dernier périphérique utilisé (clavier ou manette) avec l'ancien Input Manager.
/// Singleton auto-instancié, persistant entre les scènes. Émet OnDeviceChanged au changement.
/// </summary>
public class InputDeviceDetector : MonoBehaviour
{
    public static InputDeviceDetector Instance { get; private set; }

    public enum Device { Keyboard, Gamepad }

    public Device CurrentDevice { get; private set; } = Device.Keyboard;
    public event System.Action<Device> OnDeviceChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoBootstrap()
    {
        if (FindObjectOfType<InputDeviceDetector>() != null) return;
        var go = new GameObject("InputDeviceDetector (auto)");
        go.AddComponent<InputDeviceDetector>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Si une manette est déjà branchée et aucun clavier utilisé, on part sur Gamepad.
        foreach (var name in Input.GetJoystickNames())
        {
            if (!string.IsNullOrEmpty(name))
            {
                SetDevice(Device.Gamepad);
                break;
            }
        }
    }

    void Update()
    {
        // Les boutons de manette sont des KeyCode JoystickButton* — testés en premier.
        if (AnyJoystickButtonDown())
        {
            SetDevice(Device.Gamepad);
        }
        else if (AnyKeyboardKeyDown())
        {
            SetDevice(Device.Keyboard);
        }
    }

    private void SetDevice(Device device)
    {
        if (device == CurrentDevice) return;
        CurrentDevice = device;
        OnDeviceChanged?.Invoke(device);
    }

    private static bool AnyJoystickButtonDown()
    {
        for (KeyCode k = KeyCode.JoystickButton0; k <= KeyCode.JoystickButton19; k++)
        {
            if (Input.GetKeyDown(k)) return true;
        }
        return false;
    }

    private static bool AnyKeyboardKeyDown()
    {
        if (!Input.anyKeyDown) return false;
        // anyKeyDown inclut souris et manette : on les exclut.
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
        {
            return false;
        }
        for (KeyCode k = KeyCode.JoystickButton0; k <= KeyCode.JoystickButton19; k++)
        {
            if (Input.GetKeyDown(k)) return false;
        }
        return true;
    }
}
```

- [ ] **Step 2 : Vérification manuelle**

Lancer une scène en Play Mode. Ajouter temporairement dans un script ou via un breakpoint l'observation de `InputDeviceDetector.Instance.CurrentDevice`. Appuyer sur une touche du clavier → `Keyboard`. Appuyer sur un bouton de manette (si disponible) → `Gamepad`. Sans manette, le détecteur reste sur `Keyboard`.

- [ ] **Step 3 : Commit**

```bash
cd /Users/elouan/Desktop/WS501D/momentum-game-v2
git add Assets/Scripts/Input/InputDeviceDetector.cs
git commit -m "feat(input): add keyboard/gamepad device detector"
```

---

### Task 4 : `PlayerInput` lit les touches rebindables en WEB_BUILD

**Files:**
- Modify: `momentum-game-v2/Assets/Scripts/PlayerScripts/PlayerInput.cs:57-78`

- [ ] **Step 1 : Remplacer `Update()`**

Remplacer entièrement la méthode `Update()` (lignes 57-78) par :

```csharp
    void Update()
    {
        // --- LOGIQUE DU BUFFER DE SAUT ---
        if (jumpBufferTimer > 0)
        {
            jumpBufferTimer -= Time.deltaTime;
        }

#if WEB_BUILD
        // En web : si le device actif est le clavier, on lit UNIQUEMENT les KeyCode
        // rebindables (KeyboardControls). Si c'est la manette, on lit les axes de
        // l'Input Manager. Lire les deux séparément évite qu'une ancienne touche encore
        // câblée sur l'axe Input Manager continue de répondre après un rebinding.
        bool keyboardMode = InputDeviceDetector.Instance == null
            || InputDeviceDetector.Instance.CurrentDevice == InputDeviceDetector.Device.Keyboard;

        if (keyboardMode)
        {
            float h = 0f;
            if (Input.GetKey(KeyboardControls.Get(KeyboardControls.Action.Left)))  h -= 1f;
            if (Input.GetKey(KeyboardControls.Get(KeyboardControls.Action.Right))) h += 1f;
            HorizontalInput = h;
            VerticalInput = 0f;

            if (Input.GetKeyDown(KeyboardControls.Get(KeyboardControls.Action.Jump)))
            {
                jumpBufferTimer = jumpBufferDuration;
            }
            SlidePressed = Input.GetKeyDown(KeyboardControls.Get(KeyboardControls.Action.Slide));
            SlideHeld = Input.GetKey(KeyboardControls.Get(KeyboardControls.Action.Slide));
            LightTogglePressed = Input.GetKeyDown(KeyboardControls.Get(KeyboardControls.Action.Light));
            return;
        }
        // mode manette : on continue vers la lecture des axes ci-dessous
#endif

        if (Input.GetButtonDown(jumpButtonName))
        {
            jumpBufferTimer = jumpBufferDuration;
        }

        HorizontalInput = Input.GetAxis(horizontalAxisName);
        VerticalInput = Input.GetAxis(verticalAxisName);

        SlidePressed = Input.GetButtonDown(slideButtonName);
        SlideHeld = Input.GetButton(slideButtonName);

        LightTogglePressed = Input.GetButtonDown(lightButtonName);
    }
```

- [ ] **Step 2 : Vérification manuelle (WEB_BUILD)**

Lancer une partie web. Jouer au clavier avec les touches par défaut (flèches, Espace, Maj, E) : le joueur se déplace, saute, glisse, allume la lumière. Brancher une manette et l'utiliser : les contrôles répondent toujours. Le flux arcade (`!WEB_BUILD`) est inchangé — vérifier qu'une build arcade compile et joue normalement.

- [ ] **Step 3 : Commit**

```bash
cd /Users/elouan/Desktop/WS501D/momentum-game-v2
git add Assets/Scripts/PlayerScripts/PlayerInput.cs
git commit -m "feat(input): read rebindable keyboard keys in web build"
```

---

### Task 5 : `ControlScheme` — libellés des touches par device

**Files:**
- Create: `momentum-game-v2/Assets/Scripts/Input/ControlScheme.cs`

- [ ] **Step 1 : Créer le fichier**

```csharp
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
```

- [ ] **Step 2 : Vérifier la compilation**

Laisser recompiler dans l'éditeur. Attendu : aucune erreur.

- [ ] **Step 3 : Commit**

```bash
cd /Users/elouan/Desktop/WS501D/momentum-game-v2
git add Assets/Scripts/Input/ControlScheme.cs
git commit -m "feat(input): add ControlScheme for device-aware control hints"
```

---

### Task 6 : `ControlHintsBar` — bandeau de touches en jeu

**Files:**
- Create: `momentum-game-v2/Assets/Scripts/UI/ControlHintsBar.cs`

- [ ] **Step 1 : Créer le fichier**

```csharp
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
```

- [ ] **Step 2 : Câblage scène (manuel, scène `main`)**

Dans la scène de jeu `Assets/Scenes/main.unity` :
1. Sous le Canvas HUD, créer un objet UI `ControlHintsBar` (un `Panel` ancré en bas, étiré horizontalement, hauteur ~40px, fond semi-transparent).
2. Y ajouter un enfant `TextMeshPro - Text (UI)`, centré, taille de police ~22, couleur claire.
3. Ajouter le composant `ControlHintsBar` sur le Panel ; glisser le `TMP_Text` enfant dans le champ `hintText`.
4. Sauvegarder la scène.

- [ ] **Step 3 : Vérification manuelle**

Lancer une partie web. Le bandeau en bas affiche `←/→ Déplacer  Espace Sauter  Maj G Glisser  E Lumière`. Brancher/utiliser une manette → le bandeau bascule sur les libellés manette.

- [ ] **Step 4 : Commit**

```bash
cd /Users/elouan/Desktop/WS501D/momentum-game-v2
git add Assets/Scripts/UI/ControlHintsBar.cs Assets/Scenes/main.unity
git commit -m "feat(ui): in-game control hints bar reacting to device + rebinds"
```

---

### Task 7 : Section contrôles dans « Comment jouer »

**Files:**
- Modify: `momentum-game-v2/Assets/Scripts/HowToPlayPageUI.cs`

- [ ] **Step 1 : Ajouter un champ et le rafraîchissement**

Dans `HowToPlayPageUI`, ajouter après le bloc `[Header("Debug")]` / `showDebugLogs` (ligne 49) :

```csharp
    [Header("Contrôles")]
    [Tooltip("Texte affichant les touches du device courant (optionnel)")]
    public TMP_Text controlsHintText;
```

- [ ] **Step 2 : Rafraîchir à l'ouverture de la page**

Dans `OnEnable()`, juste avant l'appel à `UpdateUI();` (ligne 65), ajouter :

```csharp
        RefreshControlsHint();
        if (InputDeviceDetector.Instance != null)
        {
            InputDeviceDetector.Instance.OnDeviceChanged += HandleDeviceChanged;
        }
        KeyboardControls.OnChanged += RefreshControlsHint;
```

Dans `OnDisable()`, après le bloc de désabonnement du `backButton` (ligne 74), ajouter :

```csharp
        if (InputDeviceDetector.Instance != null)
        {
            InputDeviceDetector.Instance.OnDeviceChanged -= HandleDeviceChanged;
        }
        KeyboardControls.OnChanged -= RefreshControlsHint;
```

- [ ] **Step 3 : Ajouter les méthodes**

Ajouter ces deux méthodes dans la classe (par exemple juste avant `OnBackButtonClicked()`) :

```csharp
    private void HandleDeviceChanged(InputDeviceDetector.Device _) => RefreshControlsHint();

    private void RefreshControlsHint()
    {
        if (controlsHintText == null) return;
        var device = InputDeviceDetector.Instance != null
            ? InputDeviceDetector.Instance.CurrentDevice
            : InputDeviceDetector.Device.Keyboard;
        controlsHintText.text = ControlScheme.HintLine(device);
    }
```

- [ ] **Step 4 : Câblage scène (manuel, scène du menu)**

Dans la scène contenant la page « Comment jouer » (`Assets/Scenes/MainMenu.unity`) :
1. Sur le GameObject `howToPlayPage`, ajouter un `TextMeshPro - Text (UI)` visible (zone « Contrôles »).
2. Sélectionner le composant `HowToPlayPageUI`, glisser ce texte dans le champ `controlsHintText`.
3. Sauvegarder la scène.

- [ ] **Step 5 : Vérification manuelle**

Ouvrir « Comment jouer » en web : la section contrôles affiche les touches courantes. Modifier un binding (après Task 8) puis rouvrir la page → le libellé est à jour.

- [ ] **Step 6 : Commit**

```bash
cd /Users/elouan/Desktop/WS501D/momentum-game-v2
git add Assets/Scripts/HowToPlayPageUI.cs Assets/Scenes/MainMenu.unity
git commit -m "feat(ui): show device-aware control hints in How-to-Play page"
```

---

## Phase 4 — Feature 3 (UI) : écran de personnalisation des touches

### Task 8 : `ControlsSettingsUI` — écran de rebinding

**Files:**
- Create: `momentum-game-v2/Assets/Scripts/UI/ControlsSettingsUI.cs`

- [ ] **Step 1 : Créer le fichier**

```csharp
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

        foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
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
```

- [ ] **Step 2 : Câblage scène (manuel, scène du menu)**

Dans `Assets/Scenes/MainMenu.unity` :
1. Sous le Canvas du menu, créer une page `controlsSettingsPage` (un Panel plein écran), désactivée par défaut.
2. Pour chacune des 5 actions (Left, Right, Jump, Slide, Light) : une ligne avec un `TMP_Text` libellé d'action fixe (ex. « Gauche »), un `TMP_Text` pour la touche actuelle, et un `Button` « Réassigner ».
3. Ajouter un `Button` « Réinitialiser » et un `Button` « Retour ».
4. Ajouter le composant `ControlsSettingsUI` sur la page. Remplir la liste `rows` avec 5 entrées : pour chacune, choisir l'`action` dans le menu déroulant, glisser le `TMP_Text` touche dans `keyLabel`, le bouton dans `rebindButton`. Remplir `resetButton` et `backButton`.
5. Sur la page d'accueil (`homePage`), ajouter un `Button` « Contrôles ». Lui ajouter via l'événement `OnClick` un appel qui affiche la page : créer un petit script ou utiliser `MenuPageManager` — ajouter dans `MenuPageManager` une référence publique `public GameObject controlsSettingsPage;` et une méthode :

```csharp
    /// <summary>Affiche l'écran de personnalisation des touches.</summary>
    public void ShowControlsSettingsPage()
    {
        if (controlsSettingsPage != null) ShowPage(controlsSettingsPage);
    }
```

Câbler le bouton « Contrôles » de `homePage` sur `MenuPageManager.ShowControlsSettingsPage`, et renseigner le champ `controlsSettingsPage` dans l'inspecteur du `MenuPageManager`. Enregistrer aussi la page dans le dictionnaire : dans `MenuPageManager.Start()`, après la ligne `if (howToPlayPage != null) pages["howtoplay"] = howToPlayPage;`, ajouter `if (controlsSettingsPage != null) pages["controls"] = controlsSettingsPage;`.

6. Sauvegarder la scène.

- [ ] **Step 3 : Vérification manuelle**

Depuis l'accueil → bouton « Contrôles » → l'écran s'ouvre. Cliquer « Réassigner » sur « Saut » → le libellé passe à « Appuie sur une touche… » → presser `W` → le libellé devient « W ». Lancer une partie : le saut se fait avec `W`. Revenir, cliquer « Réinitialiser » → les touches reviennent aux valeurs par défaut.

- [ ] **Step 4 : Commit**

```bash
cd /Users/elouan/Desktop/WS501D/momentum-game-v2
git add Assets/Scripts/UI/ControlsSettingsUI.cs Assets/Scripts/MenuPageManager.cs Assets/Scenes/MainMenu.unity
git commit -m "feat(ui): keyboard controls rebinding screen"
```

---

## Phase 5 — Feature 4 : rejouer synchronisé

### Task 9 : Champ `wantsRematch` dans le schéma serveur

**Files:**
- Modify: `momentum-server/src/schema/PlayerState.ts`

- [ ] **Step 1 : Ajouter le champ à la classe**

Dans `PlayerState.ts`, après la ligne `actionId: number = 0;` (ligne 36, dans la classe), ajouter :

```typescript

  // Rematch : le joueur a cliqué « Rejouer » et attend l'autre.
  wantsRematch: boolean = false;
```

- [ ] **Step 2 : Ajouter le champ à `defineTypes`**

Dans le même fichier, dans l'appel `defineTypes(PlayerState, { … })`, après `actionId: "number",` (ligne 62), ajouter :

```typescript
  wantsRematch: "boolean",
```

- [ ] **Step 3 : Vérifier la compilation TypeScript**

```bash
cd /Users/elouan/buts4/www/html/SAE501/momentum-server
npx tsc --noEmit
```

Expected : aucune erreur.

- [ ] **Step 4 : Commit**

```bash
cd /Users/elouan/buts4/www/html/SAE501/momentum-server
git add src/schema/PlayerState.ts
git commit -m "feat(schema): add wantsRematch field to PlayerState"
```

---

### Task 10 : Helpers de reset rematch (TDD)

**Files:**
- Create: `momentum-server/src/rooms/rematch.ts`
- Test: `momentum-server/tests/rematch.test.ts`

- [ ] **Step 1 : Écrire le test qui échoue**

Créer `momentum-server/tests/rematch.test.ts` :

```typescript
import { describe, it, expect } from "vitest";
import { GameState } from "../src/schema/GameState";
import { PlayerState } from "../src/schema/PlayerState";
import {
  resetPlayerForRematch,
  allPlayersWantRematch,
  resetGameStateForRematch,
} from "../src/rooms/rematch";

function makePlayer(overrides: Partial<PlayerState> = {}): PlayerState {
  const p = new PlayerState();
  Object.assign(p, overrides);
  return p;
}

describe("resetPlayerForRematch", () => {
  it("remet à zéro le score, la vie, la position et le flag wantsRematch", () => {
    const p = makePlayer({
      score: 4200,
      isAlive: false,
      hasFinished: true,
      posX: 99,
      wantsRematch: true,
      playerNumber: 2,
      pseudo: "Bob",
    });

    resetPlayerForRematch(p);

    expect(p.score).toBe(0);
    expect(p.isAlive).toBe(true);
    expect(p.hasFinished).toBe(false);
    expect(p.posX).toBe(0);
    expect(p.wantsRematch).toBe(false);
    // L'identité du joueur est préservée.
    expect(p.playerNumber).toBe(2);
    expect(p.pseudo).toBe("Bob");
  });
});

describe("allPlayersWantRematch", () => {
  it("false quand un seul joueur a cliqué", () => {
    const players = [makePlayer({ wantsRematch: true }), makePlayer({ wantsRematch: false })];
    const map = { forEach: (cb: (p: PlayerState) => void) => players.forEach(cb) };
    expect(allPlayersWantRematch(map)).toBe(false);
  });

  it("true quand les deux joueurs ont cliqué", () => {
    const players = [makePlayer({ wantsRematch: true }), makePlayer({ wantsRematch: true })];
    const map = { forEach: (cb: (p: PlayerState) => void) => players.forEach(cb) };
    expect(allPlayersWantRematch(map)).toBe(true);
  });

  it("false quand il n'y a aucun joueur", () => {
    const map = { forEach: (_cb: (p: PlayerState) => void) => {} };
    expect(allPlayersWantRematch(map)).toBe(false);
  });
});

describe("resetGameStateForRematch", () => {
  it("repasse le status à loading et reset les joueurs", () => {
    const state = new GameState();
    state.status = "finished";
    state.winnerSessionId = "abc";
    state.elapsedTime = 120;
    const p = makePlayer({ score: 500, isAlive: false, wantsRematch: true });
    state.players.set("s1", p);

    resetGameStateForRematch(state);

    expect(state.status).toBe("loading");
    expect(state.winnerSessionId).toBe("");
    expect(state.elapsedTime).toBe(0);
    expect(state.players.get("s1")!.score).toBe(0);
    expect(state.players.get("s1")!.isAlive).toBe(true);
    expect(state.players.get("s1")!.wantsRematch).toBe(false);
  });
});
```

- [ ] **Step 2 : Lancer le test, vérifier qu'il échoue**

```bash
cd /Users/elouan/buts4/www/html/SAE501/momentum-server
npx vitest run tests/rematch.test.ts
```

Expected : FAIL — `Cannot find module '../src/rooms/rematch'`.

- [ ] **Step 3 : Créer l'implémentation**

Créer `momentum-server/src/rooms/rematch.ts` :

```typescript
import { GameState } from "../schema/GameState";
import { PlayerState } from "../schema/PlayerState";

/**
 * Remet un joueur dans l'état d'un début de partie, en préservant son identité
 * (playerNumber, pseudo). Appelé pour chaque joueur lors d'un rematch.
 */
export function resetPlayerForRematch(p: PlayerState): void {
  p.posX = 0;
  p.posY = 0;
  p.posZ = 0;
  p.velX = 0;
  p.velY = 0;
  p.velZ = 0;
  p.rotY = 0;
  p.isGrounded = false;
  p.isSliding = false;
  p.isStunned = false;
  p.horizontalInput = 0;
  p.score = 0;
  p.distanceTraveled = 0;
  p.survivalTime = 0;
  p.collectibles = 0;
  p.hasFinished = false;
  p.isAlive = true;
  p.isManuallySliding = false;
  p.isLandingHard = false;
  p.actionSeq = 0;
  p.actionId = 0;
  p.wantsRematch = false;
}

/**
 * Vrai si la room contient au moins un joueur et que TOUS ont demandé le rematch.
 */
export function allPlayersWantRematch(players: {
  forEach: (cb: (p: PlayerState) => void) => void;
}): boolean {
  let count = 0;
  let all = true;
  players.forEach((p) => {
    count++;
    if (!p.wantsRematch) all = false;
  });
  return count > 0 && all;
}

/**
 * Remet toute la GameState dans l'état "loading" pour relancer le handshake de
 * démarrage existant (loading → sceneReady → countdown → playing).
 */
export function resetGameStateForRematch(state: GameState): void {
  state.winnerSessionId = "";
  state.elapsedTime = 0;
  state.countdownRemaining = 0;
  state.players.forEach((p) => resetPlayerForRematch(p));
  state.status = "loading";
}
```

- [ ] **Step 4 : Lancer le test, vérifier qu'il passe**

```bash
cd /Users/elouan/buts4/www/html/SAE501/momentum-server
npx vitest run tests/rematch.test.ts
```

Expected : PASS — 5 tests passés.

- [ ] **Step 5 : Commit**

```bash
cd /Users/elouan/buts4/www/html/SAE501/momentum-server
git add src/rooms/rematch.ts tests/rematch.test.ts
git commit -m "feat(rematch): add pure reset helpers for room rematch"
```

---

### Task 11 : Handler `rematch` dans `MomentumRoom`

**Files:**
- Modify: `momentum-server/src/rooms/MomentumRoom.ts`

- [ ] **Step 1 : Importer les helpers**

Dans `MomentumRoom.ts`, après l'import de `persistScores` (ligne 11), ajouter :

```typescript
import {
  allPlayersWantRematch,
  resetGameStateForRematch,
} from "./rematch";
```

- [ ] **Step 2 : Enregistrer le message `rematch`**

Dans `onCreate()`, après la ligne `this.onMessage("death", (client: Client) => this.handleDeath(client));` (ligne 65), ajouter :

```typescript
    this.onMessage("rematch", (client: Client) => this.handleRematch(client));
```

- [ ] **Step 3 : Ajouter le handler**

Dans la classe `MomentumRoom`, après la méthode `handleDeath` (ligne 247), ajouter :

```typescript
  // Un joueur a cliqué « Rejouer » après la fin de partie. Quand les deux joueurs
  // l'ont demandé, on remet l'état à "loading" : ça relance le handshake de démarrage
  // (loading → sceneReady → countdown → playing) sur la MÊME room et la même session.
  private handleRematch(client: Client) {
    const gameState = this.state as GameState;
    if (gameState.status !== "finished") return;

    const player = gameState.players.get(client.sessionId);
    if (!player) return;

    player.wantsRematch = true;
    console.log(`[Room] P${player.playerNumber} wants rematch`);

    if (allPlayersWantRematch(gameState.players)) {
      console.log(`[Room] All players want rematch → resetting to loading`);
      this.elapsedInterval?.clear();
      this.elapsedInterval = undefined;
      this.countdownInterval?.clear();
      this.countdownInterval = undefined;
      this.sceneReadySessionIds.clear();
      resetGameStateForRematch(gameState);
    }
  }
```

- [ ] **Step 4 : Vérifier la compilation**

```bash
cd /Users/elouan/buts4/www/html/SAE501/momentum-server
npx tsc --noEmit && npx vitest run
```

Expected : compilation sans erreur, et la suite de tests existante + `rematch.test.ts` passent.

- [ ] **Step 5 : Commit**

```bash
cd /Users/elouan/buts4/www/html/SAE501/momentum-server
git add src/rooms/MomentumRoom.ts
git commit -m "feat(rematch): handle rematch message, reset room to loading"
```

---

### Task 12 : Miroir `wantsRematch` dans le schéma C#

**Files:**
- Modify: `momentum-game-v2/Assets/Scripts/Multiplayer/Schema/PlayerState.cs`

- [ ] **Step 1 : Ajouter le champ**

Dans `PlayerState.cs`, après le bloc `actionId` (lignes 84-85), avant l'accolade fermante de la classe (ligne 86), ajouter :

```csharp

	[Type(23, "boolean")]
	public bool wantsRematch = default(bool);
```

L'index `23` suit `actionId` (index 22) et doit correspondre à l'ordre d'ajout dans `defineTypes` côté serveur (Task 9, ajouté en dernier).

- [ ] **Step 2 : Vérifier la compilation**

Laisser recompiler dans l'éditeur Unity. Attendu : aucune erreur.

- [ ] **Step 3 : Commit**

```bash
cd /Users/elouan/Desktop/WS501D/momentum-game-v2
git add Assets/Scripts/Multiplayer/Schema/PlayerState.cs
git commit -m "feat(schema): mirror wantsRematch field in Unity PlayerState"
```

---

### Task 13 : `NetworkManager.SendRematch()`

**Files:**
- Modify: `momentum-game-v2/Assets/Scripts/Multiplayer/NetworkManager.cs:128-132`

- [ ] **Step 1 : Ajouter la méthode**

Dans `NetworkManager.cs`, après la ligne `public void SendDeath() => Room?.Send("death");` (ligne 132), ajouter :

```csharp
    public void SendRematch() => Room?.Send("rematch");
```

- [ ] **Step 2 : Vérifier la compilation**

Recompiler dans l'éditeur (avec `WEB_BUILD` défini). Attendu : aucune erreur.

- [ ] **Step 3 : Commit**

```bash
cd /Users/elouan/Desktop/WS501D/momentum-game-v2
git add Assets/Scripts/Multiplayer/NetworkManager.cs
git commit -m "feat(rematch): add SendRematch network message"
```

---

### Task 14 : `RematchController` + câblage du bouton Rejouer

**Files:**
- Create: `momentum-game-v2/Assets/Scripts/Multiplayer/RematchController.cs`
- Modify: `momentum-game-v2/Assets/Scripts/GameOverUI.cs`

- [ ] **Step 1 : Créer `RematchController`**

Créer `momentum-game-v2/Assets/Scripts/Multiplayer/RematchController.cs` :

```csharp
#if WEB_BUILD
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gère le « Rejouer » synchronisé en multijoueur web. Le bouton Rejouer du GameOverPanel
/// appelle RequestRematch() : on envoie le message au serveur et on passe en attente.
/// Quand les deux joueurs ont demandé le rematch, le serveur repasse status="loading"
/// → GameSessionManager.OnGameStarted fire → on recharge la scène de jeu.
/// Si l'autre joueur quitte pendant l'attente, on affiche un message et seul « Quitter » reste.
/// </summary>
public class RematchController : MonoBehaviour
{
    [Header("Nom de la scène de jeu à recharger")]
    public string gameSceneName = "main";

    [Header("UI — états du rematch")]
    [Tooltip("Bouton Rejouer (caché une fois cliqué)")]
    public GameObject rematchButton;
    [Tooltip("Message « En attente de l'autre joueur… »")]
    public GameObject waitingMessage;
    [Tooltip("Message « L'autre joueur a quitté la partie »")]
    public GameObject opponentLeftMessage;

    private bool _waitingForRematch = false;
    private bool _reloadTriggered = false;

    void OnEnable()
    {
        GameSessionManager.OnGameStarted += HandleGameStarted;
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnPlayerRemoved += HandlePlayerRemoved;
        }
    }

    void OnDisable()
    {
        GameSessionManager.OnGameStarted -= HandleGameStarted;
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnPlayerRemoved -= HandlePlayerRemoved;
        }
    }

    /// <summary>Appelé par le bouton « Rejouer » du GameOverPanel.</summary>
    public void RequestRematch()
    {
        if (_waitingForRematch) return;
        _waitingForRematch = true;

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.SendRematch();
        }

        if (rematchButton != null) rematchButton.SetActive(false);
        if (waitingMessage != null) waitingMessage.SetActive(true);
        if (opponentLeftMessage != null) opponentLeftMessage.SetActive(false);
    }

    // Le serveur a repassé status="loading" (les deux joueurs veulent rejouer) →
    // GameSessionManager fire OnGameStarted. On recharge la scène de jeu.
    private void HandleGameStarted()
    {
        if (_reloadTriggered) return;
        _reloadTriggered = true;
        SceneManager.LoadScene(gameSceneName);
    }

    // Un joueur a quitté. Si on attendait le rematch, c'est une annulation : on prévient.
    private void HandlePlayerRemoved(string _)
    {
        if (!_waitingForRematch || _reloadTriggered) return;

        if (waitingMessage != null) waitingMessage.SetActive(false);
        if (opponentLeftMessage != null) opponentLeftMessage.SetActive(true);
        if (rematchButton != null) rematchButton.SetActive(false);
    }
}
#endif
```

- [ ] **Step 2 : Router le bouton Rejouer via `RematchController` en WEB_BUILD**

Dans `GameOverUI.cs`, remplacer la méthode `RestartGame()` (lignes 6-18) par :

```csharp
    public void RestartGame()
    {
#if WEB_BUILD
        // En multijoueur web, « Rejouer » est synchronisé : on passe par RematchController
        // au lieu de recharger la scène localement (sinon l'autre joueur ne suit pas).
        var rematch = FindObjectOfType<RematchController>();
        if (rematch != null)
        {
            rematch.RequestRematch();
            return;
        }
        Debug.LogWarning("[GameOverUI] RematchController introuvable — fallback reload local");
#endif
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
        else
        {
            // Fallback if GameManager is missing
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
```

- [ ] **Step 3 : Câblage scène (manuel, scène `main`)**

Dans `Assets/Scenes/main.unity` :
1. Sur un GameObject persistant de la scène (ex. l'objet portant `GameManager`), ajouter le composant `RematchController`.
2. Sur le `GameOverPanel`, ajouter deux éléments texte cachés par défaut : un `waitingMessage` (« En attente de l'autre joueur… ») et un `opponentLeftMessage` (« L'autre joueur a quitté la partie »).
3. Dans l'inspecteur de `RematchController` : `gameSceneName` = `main` ; `rematchButton` = le GameObject du bouton « Rejouer » du `GameOverPanel` ; `waitingMessage` et `opponentLeftMessage` = les deux textes ci-dessus.
4. Vérifier que le bouton « Rejouer » du `GameOverPanel` a bien son `OnClick` câblé sur `GameOverUI.RestartGame` (inchangé — le routage se fait dans le code).
5. Le bouton « Quitter » reste câblé sur `GameOverUI.QuitGame` — il reste utilisable dans l'état « l'autre joueur a quitté ».
6. Sauvegarder la scène.

- [ ] **Step 4 : Vérification manuelle (deux clients web)**

Lancer une partie web à deux joueurs jusqu'à la fin. Sur le GameOverPanel :
- Joueur A clique « Rejouer » → A voit « En attente de l'autre joueur… », le bouton Rejouer disparaît.
- Joueur B clique « Rejouer » → les deux clients rechargent la scène `main` et une nouvelle partie démarre (compte à rebours), sur la **même session** (même code dans l'URL `/classement/...`).
- Cas abandon : refaire une partie, A clique « Rejouer », puis B ferme son onglet → A voit « L'autre joueur a quitté la partie » et ne peut plus que cliquer « Quitter ».

- [ ] **Step 5 : Commit**

```bash
cd /Users/elouan/Desktop/WS501D/momentum-game-v2
git add Assets/Scripts/Multiplayer/RematchController.cs Assets/Scripts/GameOverUI.cs Assets/Scenes/main.unity
git commit -m "feat(rematch): synchronized replay button in web multiplayer"
```

---

### Task 15 : Rendre `/api/game/end` idempotent (rematch-safe)

**Files:**
- Modify: `momentum/src/app/api/game/end/route.ts:81-102`

**Contexte :** `/api/game/end` fait `prisma.score.create()` — un *insert* à chaque appel. Après un rematch, chaque client re-POSTe ses scores → des lignes en double pour la même `gameSessionId`, et le classement de la session afficherait plusieurs parties mélangées. On veut que le classement reflète la **dernière** partie jouée sur la session : on supprime les scores existants de la session avant de recréer.

- [ ] **Step 1 : Supprimer les scores existants avant de recréer**

Dans `route.ts`, juste avant le bloc `// Créer les scores pour chaque joueur` / `const createdScores = await Promise.all(` (ligne 81), insérer :

```typescript
    // Rematch-safe : une session peut rejouer plusieurs parties. On efface les scores
    // de la session avant de réécrire ceux de la partie qui vient de se terminer, pour
    // que le classement reflète la dernière partie et non un cumul de doublons.
    await prisma.score.deleteMany({ where: { gameSessionId: session.id } });

```

- [ ] **Step 2 : Vérifier la compilation / le lint**

```bash
cd /Users/elouan/buts4/www/html/SAE501/momentum
npx next lint --file src/app/api/game/end/route.ts
```

Expected : aucune erreur de lint sur ce fichier.

- [ ] **Step 3 : Vérification manuelle**

Jouer une partie web jusqu'à la fin, puis faire un rematch (Task 14) et finir la seconde partie. Ouvrir `/classement/[sessionId]` : exactement **2 lignes de score** (une par joueur), correspondant à la dernière partie — pas 4.

- [ ] **Step 4 : Commit**

```bash
cd /Users/elouan/buts4/www/html/SAE501/momentum
git add src/app/api/game/end/route.ts
git commit -m "fix(api): make /api/game/end idempotent for session rematch"
```

---

## Récapitulatif des dépendances entre tâches

- Task 1 : indépendante.
- Task 4 dépend de Task 2 (`KeyboardControls`) et Task 3 (`InputDeviceDetector`).
- Task 5 dépend de Task 2 et Task 3.
- Task 6 et Task 7 dépendent de Task 5.
- Task 8 dépend de Task 2.
- Task 11 dépend de Task 9 et Task 10.
- Task 12 doit être faite avec Task 9 (l'ordre des champs du schéma C# et serveur doit rester synchronisé).
- Task 13 et Task 14 dépendent de Task 12 (sinon désérialisation cassée) et Task 11 (handler serveur).
- Task 15 : indépendante, mais nécessaire pour que le rematch (Task 14) ne pollue pas le classement.
