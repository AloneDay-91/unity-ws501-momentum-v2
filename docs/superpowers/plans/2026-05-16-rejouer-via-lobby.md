# Rejouer via le lobby — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Faire que le bouton « Rejouer » ramène les joueurs sur l'interface du lobby (« en attente de l'autre joueur ») puis relance automatiquement la partie quand les deux ont cliqué.

**Architecture:** Un flag statique `RematchState` signale au menu qu'on revient pour un rejouer. `GameOverUI` envoie le message `rematch` au serveur puis charge la scène menu. `MenuPageManager` affiche alors la `LobbyPage` en mode rematch-attente. La `LobbyPage` réutilise son abonnement existant à `OnGameStarted` pour recharger `main` quand le serveur relance la partie. L'ancien `RematchController` est supprimé.

**Tech Stack:** Unity (C#, Colyseus SDK). Repo : `momentum-game-v2` (`/Users/elouan/Desktop/WS501D/momentum-game-v2`), branche `main` (commits directs autorisés par l'utilisateur).

**Notes :**
- Projet Unity sans framework de tests → vérification manuelle (à deux clients web).
- Le code sous `#if WEB_BUILD` ne compile qu'avec le symbole `WEB_BUILD` dans Player Settings → Scripting Define Symbols.
- Ne pas créer de fichiers `.cs.meta` — Unity les génère.
- Le serveur (`momentum-server`) est déjà prêt (handler `rematch` + `resetGameStateForRematch`), aucune modification serveur.
- Spec : `docs/superpowers/specs/2026-05-16-rejouer-via-lobby-design.md`.

---

### Task 1 : Flag `RematchState`

**Files:**
- Create: `Assets/Scripts/Multiplayer/RematchState.cs`

- [ ] **Step 1 : Créer le fichier**

```csharp
/// <summary>
/// Signale qu'on revient à la scène menu pour un « Rejouer » : MenuPageManager doit
/// alors afficher directement la LobbyPage en mode attente au lieu de la page d'accueil.
/// Faux par défaut ; levé par GameOverUI, consommé par MenuPageManager.
/// </summary>
public static class RematchState
{
    public static bool ReturningForRematch = false;
}
```

- [ ] **Step 2 : Vérifier la compilation**

Revenir dans l'éditeur Unity, laisser recompiler. Attendu : aucune erreur.

- [ ] **Step 3 : Commit**

```bash
cd /Users/elouan/Desktop/WS501D/momentum-game-v2
git add Assets/Scripts/Multiplayer/RematchState.cs
git commit -m "$(cat <<'EOF'
feat(rematch): add RematchState flag for return-to-lobby flow

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 2 : `LobbyPageUI` — mode rematch-attente

**Files:**
- Modify: `Assets/Scripts/LobbyPageUI.cs`

- [ ] **Step 1 : Ajouter les champs d'état et `EnterRematchMode()`**

Dans `LobbyPageUI`, après le bloc `[Header("Debug")] public bool showDebug = true;` (ligne 41), ajouter :

```csharp

    // Mode « rejouer » : la page a été ouverte en retour d'un clic « Rejouer ».
    private bool _rematchMode = false;
    private bool _opponentLeft = false;

    /// <summary>Appelé par MenuPageManager avant d'afficher le lobby en retour de « Rejouer ».</summary>
    public void EnterRematchMode()
    {
        _rematchMode = true;
    }
```

- [ ] **Step 2 : Abonner/désabonner `OnPlayerRemoved` et gérer la course de démarrage**

Remplacer entièrement la méthode `OnEnable()` (lignes 43-56) par :

```csharp
    void OnEnable()
    {
        // S'abonne à l'événement de démarrage de partie
        GameSessionManager.OnGameStarted += OnGameStarted;

        // S'abonne aux events de pseudo/ready pour rafraîchir l'UI quand
        // les noms arrivent (depuis le polling API en arcade, depuis OnPlayerAdded en WEB_BUILD)
        GameSessionManager.OnPlayer1Joined += OnPlayerInfoChanged;
        GameSessionManager.OnPlayer2Joined += OnPlayerInfoChanged;
        GameSessionManager.OnBothPlayersReady += OnBothPlayersReady;

#if WEB_BUILD
        if (_rematchMode && NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnPlayerRemoved += HandlePlayerRemovedDuringRematch;
        }
#endif

        // Rafraîchit l'affichage quand la page s'active
        UpdateDisplay();

#if WEB_BUILD
        // Course possible : l'autre joueur peut avoir déclenché le redémarrage serveur
        // pendant le chargement de cette scène — on aurait alors raté l'event OnGameStarted.
        if (_rematchMode) CheckRematchAlreadyStarted();
#endif
    }
```

Remplacer entièrement la méthode `OnDisable()` (lignes 58-65) par :

```csharp
    void OnDisable()
    {
        // Se désabonne des events
        GameSessionManager.OnGameStarted -= OnGameStarted;
        GameSessionManager.OnPlayer1Joined -= OnPlayerInfoChanged;
        GameSessionManager.OnPlayer2Joined -= OnPlayerInfoChanged;
        GameSessionManager.OnBothPlayersReady -= OnBothPlayersReady;

#if WEB_BUILD
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnPlayerRemoved -= HandlePlayerRemovedDuringRematch;
        }
#endif
    }
```

- [ ] **Step 3 : Ajouter les méthodes rematch**

Juste après la méthode `OnDisable()`, ajouter :

```csharp
#if WEB_BUILD
    // L'autre joueur a quitté pendant qu'on attendait son « Rejouer ».
    private void HandlePlayerRemovedDuringRematch(string _)
    {
        if (!_rematchMode) return;
        _opponentLeft = true;
        if (showDebug) Debug.Log("LobbyPageUI: l'autre joueur a quitté pendant l'attente de rejouer");
        UpdateDisplay();
    }

    // Si le serveur a déjà relancé la partie avant que ce lobby ne s'abonne, on rattrape.
    private void CheckRematchAlreadyStarted()
    {
        string status = NetworkManager.Instance?.Room?.State?.status;
        if (status == "loading" || status == "countdown" || status == "playing")
        {
            if (showDebug) Debug.Log($"LobbyPageUI: rematch déjà démarré (status={status}) → LoadScene");
            OnGameStarted();
        }
    }
#endif
```

- [ ] **Step 4 : Adapter `UpdateDisplay()` pour le mode rematch**

Dans `UpdateDisplay()`, remplacer le bloc « Bouton start » (lignes 129-146, de `// Bouton start` jusqu'à la fin du `if (startGameButton != null) { ... }`) par :

```csharp
        // Bouton start
        if (startGameButton != null)
        {
            if (_rematchMode)
            {
                // Redémarrage automatique : le bouton reste désactivé et sert d'indicateur d'attente.
                startGameButton.interactable = false;
                if (startButtonText != null)
                {
                    startButtonText.text = _opponentLeft
                        ? "L'autre joueur a quitté la partie"
                        : "En attente de l'autre joueur…";
                }
            }
            else
            {
                bool canStart = GameSessionManager.Instance.bothPlayersReady;
                startGameButton.interactable = canStart;

                if (startButtonText != null)
                {
                    if (canStart)
                    {
                        startButtonText.text = "DÉMARRER LA PARTIE";
                    }
                    else
                    {
                        startButtonText.text = "En attente des joueurs...";
                    }
                }
            }
        }
```

- [ ] **Step 5 : Garder `OnStartGameClicked` inerte en mode rematch**

Dans `OnStartGameClicked()`, ajouter comme **toute première instruction** de la méthode (avant `if (!GameSessionManager.Instance.bothPlayersReady)`) :

```csharp
        if (_rematchMode) return;
```

- [ ] **Step 6 : Bouton « Retour » en mode rematch → retour au site**

Remplacer entièrement la méthode `OnBackButtonClicked()` par :

```csharp
    /// <summary>
    /// Retour au menu. En WEB_BUILD la session vient de l'URL et la connexion Colyseus
    /// est persistante — on ne la détruit PAS, on navigue juste vers la home. En mode
    /// rematch, « Retour » annule le rejouer et renvoie au site (page de classement).
    /// </summary>
    public void OnBackButtonClicked()
    {
        if (MenuPageManager.Instance == null) return;

#if WEB_BUILD
        if (_rematchMode)
        {
            if (showDebug) Debug.Log("LobbyPageUI: rematch annulé → retour au site");
            WebBridge.NotifyQuit(WebBootstrap.SessionId);
            return;
        }
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

- [ ] **Step 7 : Vérifier la compilation**

Laisser recompiler dans l'éditeur (avec `WEB_BUILD` défini). Attendu : aucune erreur.

- [ ] **Step 8 : Commit**

```bash
cd /Users/elouan/Desktop/WS501D/momentum-game-v2
git add Assets/Scripts/LobbyPageUI.cs
git commit -m "$(cat <<'EOF'
feat(rematch): rematch-waiting mode in LobbyPageUI

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 3 : `MenuPageManager` affiche le lobby en retour de rejouer

**Files:**
- Modify: `Assets/Scripts/MenuPageManager.cs` (méthode `Start()`)

- [ ] **Step 1 : Consommer le flag dans `Start()`**

Dans `MenuPageManager.cs`, méthode `Start()`, le bloc actuel après le masquage des pages est :

```csharp
        // Affiche la page de départ (en WEB_BUILD comme en arcade : home page).
        // En WEB_BUILD, le bouton Play / ShowQRCodePage sera redirigé vers la LobbyPage
        // pour skipper la génération de QR code (inutile : la session vient déjà de l'URL).
        if (startingPage != null)
        {
            ShowPage(startingPage);
        }
        else if (homePage != null)
        {
            ShowPage(homePage);
        }
```

Le remplacer par :

```csharp
        // Retour d'un « Rejouer » : on consomme le flag et on va droit au lobby en mode
        // attente, au lieu d'afficher la page d'accueil.
        bool rematch = RematchState.ReturningForRematch;
        RematchState.ReturningForRematch = false;

        if (rematch && lobbyPage != null)
        {
            var lobbyUI = lobbyPage.GetComponent<LobbyPageUI>();
            if (lobbyUI != null) lobbyUI.EnterRematchMode();
            ShowPage(lobbyPage);
            return;
        }

        // Affiche la page de départ (en WEB_BUILD comme en arcade : home page).
        // En WEB_BUILD, le bouton Play / ShowQRCodePage sera redirigé vers la LobbyPage
        // pour skipper la génération de QR code (inutile : la session vient déjà de l'URL).
        if (startingPage != null)
        {
            ShowPage(startingPage);
        }
        else if (homePage != null)
        {
            ShowPage(homePage);
        }
```

- [ ] **Step 2 : Vérifier la compilation**

Laisser recompiler dans l'éditeur. Attendu : aucune erreur (`LobbyPageUI.EnterRematchMode()` existe depuis la Task 2).

- [ ] **Step 3 : Commit**

```bash
cd /Users/elouan/Desktop/WS501D/momentum-game-v2
git add Assets/Scripts/MenuPageManager.cs
git commit -m "$(cat <<'EOF'
feat(rematch): show lobby in rematch mode on return from replay

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 4 : `GameOverUI` — « Rejouer » envoie le rematch et va au lobby

**Files:**
- Modify: `Assets/Scripts/GameOverUI.cs` (méthode `RestartGame()`)

- [ ] **Step 1 : Remplacer `RestartGame()`**

Dans `GameOverUI.cs`, remplacer entièrement la méthode `RestartGame()` par :

```csharp
    public void RestartGame()
    {
#if WEB_BUILD
        if (!DevSolo.Active)
        {
            // Rejouer synchronisé : on prévient le serveur et on retourne sur le lobby
            // (« en attente de l'autre joueur »). Quand les deux joueurs ont cliqué,
            // le serveur relance la partie et le lobby recharge "main" automatiquement.
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.SendRematch();
            }
            RematchState.ReturningForRematch = true;
            SceneManager.LoadScene("MainMenu");
            return;
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

La méthode `QuitGame()` reste inchangée. Le mode DevSolo et le flux arcade (`#if WEB_BUILD` faux) gardent le rechargement local.

- [ ] **Step 2 : Vérifier la compilation**

Laisser recompiler dans l'éditeur (avec `WEB_BUILD` défini). Attendu : aucune erreur. `RematchController` n'est plus référencé.

- [ ] **Step 3 : Commit**

```bash
cd /Users/elouan/Desktop/WS501D/momentum-game-v2
git add Assets/Scripts/GameOverUI.cs
git commit -m "$(cat <<'EOF'
feat(rematch): replay button sends rematch and returns to lobby

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 5 : Supprimer l'ancien `RematchController`

**Files:**
- Delete: `Assets/Scripts/Multiplayer/RematchController.cs`

- [ ] **Step 1 : Vérifier qu'il n'est plus référencé**

```bash
cd /Users/elouan/Desktop/WS501D/momentum-game-v2
grep -rn "RematchController" Assets/Scripts/
```

Expected : aucune occurrence (après les Tasks 2-4, plus rien ne référence `RematchController`).
Si une occurrence subsiste, STOP et signaler — ne pas supprimer le fichier.

- [ ] **Step 2 : Supprimer le fichier**

```bash
cd /Users/elouan/Desktop/WS501D/momentum-game-v2
git rm Assets/Scripts/Multiplayer/RematchController.cs
rm -f Assets/Scripts/Multiplayer/RematchController.cs.meta
```

(`RematchController.cs.meta` est généré par Unity et peut être non suivi par git — `rm -f` le supprime sans erreur s'il est absent.)

- [ ] **Step 3 : Vérifier la compilation**

Laisser recompiler dans l'éditeur. Attendu : aucune erreur.

- [ ] **Step 4 : Commit**

```bash
cd /Users/elouan/Desktop/WS501D/momentum-game-v2
git add -A Assets/Scripts/Multiplayer/RematchController.cs
git commit -m "$(cat <<'EOF'
chore(rematch): remove unused RematchController, replaced by lobby flow

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

- [ ] **Step 5 : Vérification manuelle de l'ensemble (deux clients web)**

Avec le serveur Colyseus et l'API lancés, deux clients web :
1. Jouer une partie jusqu'à la fin (game over panel sur les deux clients).
2. Joueur A clique « Rejouer » → A arrive sur la `LobbyPage`, le bouton affiche « En attente de l'autre joueur… » (désactivé).
3. Joueur B clique « Rejouer » → B arrive aussi sur le lobby ; quasi aussitôt les **deux** clients rechargent `main` et une nouvelle partie démarre (compte à rebours), sur la même session.
4. Cas abandon : refaire une partie, A clique « Rejouer », B ferme son onglet → A voit « L'autre joueur a quitté la partie » ; le bouton « Retour » de A le ramène au site.
5. Non-régression : un premier démarrage normal (accueil → Démarrer) affiche toujours le lobby normal avec le bouton « DÉMARRER LA PARTIE ».

---

## Dépendances entre tâches

- Task 1 (`RematchState`) en premier — référencé par les Tasks 3 et 4.
- Task 2 (`LobbyPageUI.EnterRematchMode`) avant Task 3 — `MenuPageManager` appelle `EnterRematchMode()`, le commit de Task 3 doit compiler.
- Task 4 (`GameOverUI`) retire la dernière référence à `RematchController` ; Task 5 (suppression) doit donc venir après Task 4.
- Ordre : 1 → 2 → 3 → 4 → 5.
