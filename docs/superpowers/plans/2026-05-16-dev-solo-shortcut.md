# Raccourci dev solo offline — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Permettre, en développement uniquement, de sauter directement dans une partie solo hors-ligne de la build WEB_BUILD (F9 depuis le menu, ou Play direct sur la scène `main`), sans serveur Colyseus.

**Architecture:** Un flag statique `DevSolo.Active` (faux par défaut, jamais activé en build livrée). Un lanceur `#if UNITY_EDITOR` auto-instancié le passe à `true` via F9 ou via détection « scène de boot = main ». Quatre points du code WEB_BUILD reçoivent une garde `if (DevSolo.Active)` qui court-circuite le réseau et utilise les chemins locaux existants.

**Tech Stack:** Unity (C#, ancien Input Manager, Colyseus SDK). Repo : `momentum-game-v2` (`/Users/elouan/Desktop/WS501D/momentum-game-v2`), branche `main` (commits directs autorisés par l'utilisateur).

**Notes :**
- Ce projet Unity n'a pas de framework de tests. Les vérifications sont manuelles, en éditeur. Le code sous `#if WEB_BUILD` ne compile que si le symbole `WEB_BUILD` est dans Player Settings → Scripting Define Symbols.
- Ne pas créer de fichiers `.cs.meta` — Unity les génère à la prochaine ouverture.
- Spec de référence : `docs/superpowers/specs/2026-05-16-dev-solo-shortcut-design.md`.

---

### Task 1 : Flag statique `DevSolo`

**Files:**
- Create: `Assets/Scripts/Dev/DevSolo.cs`

- [ ] **Step 1 : Créer le dossier et le fichier**

Créer le dossier `Assets/Scripts/Dev/` s'il n'existe pas, puis créer `Assets/Scripts/Dev/DevSolo.cs` :

```csharp
/// <summary>
/// Flag dev-only. Quand Active est true, la build WEB_BUILD démarre une partie solo
/// hors-ligne (aucune connexion Colyseus). Seul DevSoloLauncher (#if UNITY_EDITOR) le
/// passe à true ; dans une build livrée il reste false et tous les branchements
/// `if (DevSolo.Active)` sont du code mort inactif.
/// </summary>
public static class DevSolo
{
    public static bool Active = false;
}
```

- [ ] **Step 2 : Vérifier la compilation**

Revenir dans l'éditeur Unity, laisser recompiler. Attendu : aucune erreur.

- [ ] **Step 3 : Commit**

```bash
cd /Users/elouan/Desktop/WS501D/momentum-game-v2
git add Assets/Scripts/Dev/DevSolo.cs
git commit -m "$(cat <<'EOF'
feat(dev): add DevSolo flag for offline solo web build

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 2 : Lanceur `DevSoloLauncher` (éditeur uniquement)

**Files:**
- Create: `Assets/Scripts/Dev/DevSoloLauncher.cs`

- [ ] **Step 1 : Créer le fichier**

Créer `Assets/Scripts/Dev/DevSoloLauncher.cs` :

```csharp
#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Éditeur uniquement. Active le mode solo dev (DevSolo.Active) de deux façons :
///  - F9 depuis une scène ≠ "main" → passe le flag et charge "main".
///  - Au lancement, si la scène de boot est déjà "main" (le dev a ouvert main.unity
///    et pressé Play) → solo automatique.
/// N'existe jamais dans une build livrée (#if UNITY_EDITOR).
/// </summary>
public class DevSoloLauncher : MonoBehaviour
{
    private const string GameSceneName = "main";

    // Détection « boot direct sur main ». AfterSceneLoad s'exécute après le chargement
    // de la scène initiale mais AVANT les Start(), donc avant que NetworkManager,
    // GameSessionManager et GameManager ne lisent DevSolo.Active.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void DetectBootScene()
    {
        if (SceneManager.GetActiveScene().name == GameSceneName)
        {
            DevSolo.Active = true;
            Debug.Log("[DevSoloLauncher] Boot direct sur 'main' → mode solo dev activé");
        }
    }

    // Crée le composant persistant qui écoute F9.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoBootstrap()
    {
        if (FindObjectOfType<DevSoloLauncher>() != null) return;
        var go = new GameObject("DevSoloLauncher (editor)");
        go.AddComponent<DevSoloLauncher>();
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9)
            && SceneManager.GetActiveScene().name != GameSceneName)
        {
            DevSolo.Active = true;
            Debug.Log("[DevSoloLauncher] F9 → chargement de 'main' en mode solo dev");
            SceneManager.LoadScene(GameSceneName);
        }
    }
}
#endif
```

- [ ] **Step 2 : Vérifier la compilation**

Laisser recompiler dans l'éditeur. Attendu : aucune erreur.

- [ ] **Step 3 : Commit**

```bash
cd /Users/elouan/Desktop/WS501D/momentum-game-v2
git add Assets/Scripts/Dev/DevSoloLauncher.cs
git commit -m "$(cat <<'EOF'
feat(dev): editor-only launcher for solo mode (F9 + boot-into-main)

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 3 : `NetworkManager` saute la connexion en mode solo

**Files:**
- Modify: `Assets/Scripts/Multiplayer/NetworkManager.cs` (méthode `Start()`)

- [ ] **Step 1 : Ajouter la garde au début de `Start()`**

Dans `NetworkManager.cs`, la méthode `async void Start()` commence actuellement par une ligne `Debug.Log($"[DIAG][NetworkManager] Start fired ...")`. Ajouter, comme **toute première instruction** de `Start()` (avant ce `Debug.Log`) :

```csharp
        if (DevSolo.Active)
        {
            Debug.Log("[NetworkManager] DevSolo actif — connexion Colyseus ignorée");
            return;
        }
```

Le reste de `Start()` est inchangé. Tout le fichier est déjà sous `#if WEB_BUILD`, donc aucune directive supplémentaire n'est nécessaire.

- [ ] **Step 2 : Vérifier la compilation**

Laisser recompiler dans l'éditeur (avec `WEB_BUILD` défini). Attendu : aucune erreur.

- [ ] **Step 3 : Commit**

```bash
cd /Users/elouan/Desktop/WS501D/momentum-game-v2
git add Assets/Scripts/Multiplayer/NetworkManager.cs
git commit -m "$(cat <<'EOF'
feat(dev): skip Colyseus connection when DevSolo is active

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 4 : `GameSessionManager` configure P1 local sans réseau

**Files:**
- Modify: `Assets/Scripts/GameSessionManager.cs` (méthodes `InitWebMode()` et `SetupLocalPlayer()`)

- [ ] **Step 1 : Ajouter un paramètre optionnel à `SetupLocalPlayer`**

Dans `GameSessionManager.cs`, la méthode `SetupLocalPlayer` est déclarée :

```csharp
    private void SetupLocalPlayer(int playerNumber)
```

La remplacer par :

```csharp
    private void SetupLocalPlayer(int playerNumber, bool attachSync = true)
```

Puis, dans le corps de cette méthode, le bloc qui attache `LocalPlayerSync` :

```csharp
        if (mine.GetComponent<LocalPlayerSync>() == null)
            mine.AddComponent<LocalPlayerSync>();
```

devient :

```csharp
        if (attachSync && mine.GetComponent<LocalPlayerSync>() == null)
            mine.AddComponent<LocalPlayerSync>();
```

`LocalPlayerSync` envoie l'état au serveur ; en solo hors-ligne il est inutile, on ne l'attache pas.

- [ ] **Step 2 : Ajouter la branche DevSolo dans `InitWebMode()`**

Toujours dans `GameSessionManager.cs`, méthode `InitWebMode()`. Après la ligne qui réinitialise le flag d'initialisation :

```csharp
        _localPlayerInitialized = false;
```

insérer juste après :

```csharp

        // Mode solo dev hors-ligne : pas de serveur pour assigner un slot joueur.
        // On configure directement P1 comme joueur local et on saute tout le wiring réseau.
        if (DevSolo.Active)
        {
            if (showDebug) Debug.Log("[GameSessionManager] DevSolo — configuration P1 local, sans réseau");
            _localPlayerNumber = 1;
            SetupLocalPlayer(1, attachSync: false);
            return;
        }
```

Le `return` saute l'abonnement aux événements `NetworkManager` et la phase de catch-up — inutiles hors-ligne.

- [ ] **Step 3 : Vérifier la compilation**

Laisser recompiler dans l'éditeur (avec `WEB_BUILD` défini). Attendu : aucune erreur.

- [ ] **Step 4 : Commit**

```bash
cd /Users/elouan/Desktop/WS501D/momentum-game-v2
git add Assets/Scripts/GameSessionManager.cs
git commit -m "$(cat <<'EOF'
feat(dev): set up P1 as offline local player when DevSolo is active

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 5 : `GameManager` utilise le compte à rebours local en mode solo

**Files:**
- Modify: `Assets/Scripts/GameManager.cs` (bloc `#if WEB_BUILD` de `Start()`, lignes ~95-103)

- [ ] **Step 1 : Remplacer le bloc de démarrage du compte à rebours**

Dans `GameManager.cs`, méthode `Start()`, le bloc actuel est :

```csharp
#if WEB_BUILD
        // Multiplayer: countdown is driven by the server (state.status: loading → countdown → playing).
        // The local 3s timer would race with the other client; we follow the server clock instead.
        GameSessionManager.OnGameFinished -= HandleServerGameFinished;
        GameSessionManager.OnGameFinished += HandleServerGameFinished;
        StartCoroutine(ServerDrivenCountdownCoroutine());
#else
        StartCoroutine(StartCountdownCoroutine());
#endif
```

Le remplacer par :

```csharp
#if WEB_BUILD
        if (DevSolo.Active)
        {
            // Mode solo dev hors-ligne : aucun serveur → compte à rebours local.
            StartCoroutine(StartCountdownCoroutine());
        }
        else
        {
            // Multiplayer: countdown is driven by the server (state.status: loading → countdown → playing).
            // The local 3s timer would race with the other client; we follow the server clock instead.
            GameSessionManager.OnGameFinished -= HandleServerGameFinished;
            GameSessionManager.OnGameFinished += HandleServerGameFinished;
            StartCoroutine(ServerDrivenCountdownCoroutine());
        }
#else
        StartCoroutine(StartCountdownCoroutine());
#endif
```

`StartCountdownCoroutine()` est déjà défini sans condition de compilation dans `GameManager.cs` (le compte à rebours local 3-2-1-GO, sans réseau).

- [ ] **Step 2 : Vérifier la compilation**

Laisser recompiler dans l'éditeur (avec `WEB_BUILD` défini). Attendu : aucune erreur.

- [ ] **Step 3 : Commit**

```bash
cd /Users/elouan/Desktop/WS501D/momentum-game-v2
git add Assets/Scripts/GameManager.cs
git commit -m "$(cat <<'EOF'
feat(dev): use local countdown in web build when DevSolo is active

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 6 : `GameOverUI` rechargement local en mode solo

**Files:**
- Modify: `Assets/Scripts/GameOverUI.cs` (méthode `RestartGame()`)

- [ ] **Step 1 : Remplacer `RestartGame()`**

Dans `GameOverUI.cs`, remplacer entièrement la méthode `RestartGame()` par :

```csharp
    public void RestartGame()
    {
#if WEB_BUILD
        // En multijoueur web, « Rejouer » est synchronisé via RematchController.
        // En mode solo dev (hors-ligne) il n'y a pas de réseau : on saute le rematch
        // et on recharge la scène en local.
        if (!DevSolo.Active)
        {
            var rematch = FindObjectOfType<RematchController>();
            if (rematch != null)
            {
                rematch.RequestRematch();
                return;
            }
            Debug.LogError("[GameOverUI] RematchController introuvable — fallback reload local");
        }
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

La méthode `QuitGame()` reste inchangée.

- [ ] **Step 2 : Vérifier la compilation**

Laisser recompiler dans l'éditeur (avec `WEB_BUILD` défini). Attendu : aucune erreur.

- [ ] **Step 3 : Vérification manuelle de l'ensemble**

Dans l'éditeur, avec le symbole `WEB_BUILD` défini :

1. **Test F9 :** ouvrir `Assets/Scenes/MainMenu.unity`, presser Play, puis presser **F9**. Attendu : la scène `main` se charge, le compte à rebours local 3-2-1-GO se lance, on contrôle P1, P2 est désactivé, caméra plein écran. Aucune erreur console liée à une `Room`/serveur manquant.
2. **Test auto :** ouvrir `Assets/Scenes/main.unity`, presser Play. Attendu : même résultat, sans presser F9 (log `[DevSoloLauncher] Boot direct sur 'main'`).
3. **Test Rejouer :** en fin de partie, cliquer « Rejouer ». Attendu : la scène `main` se recharge en solo, nouvelle partie.
4. **Non-régression :** vérifier qu'en partant du menu sans presser F9 (flux normal), `DevSolo.Active` reste faux et le jeu suit le flux serveur habituel.

- [ ] **Step 4 : Commit**

```bash
cd /Users/elouan/Desktop/WS501D/momentum-game-v2
git add Assets/Scripts/GameOverUI.cs
git commit -m "$(cat <<'EOF'
feat(dev): local restart on replay when DevSolo is active

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Dépendances entre tâches

- Tasks 3, 4, 5, 6 référencent `DevSolo.Active` → Task 1 doit être faite en premier.
- Task 2 (le lanceur) référence `DevSolo` → après Task 1.
- Tasks 3-6 sont indépendantes entre elles et peuvent être faites dans n'importe quel ordre après Task 1.
