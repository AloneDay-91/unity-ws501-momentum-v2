# Design — Rejouer via l'interface du lobby

Date : 2026-05-16
Repo : `momentum-game-v2` (client Unity). Serveur `momentum-server` : inchangé (le
handler `rematch` et `resetGameStateForRematch` sont déjà implémentés et déployés).

## Objectif

Quand un joueur clique « Rejouer » sur l'écran de fin de partie, il doit retrouver la
**même interface qu'au premier démarrage** : la `LobbyPage` (« en attente de l'autre
joueur »). Quand les deux joueurs ont cliqué Rejouer, la partie se relance
**automatiquement** sur la même session.

Cette approche remplace l'ancien `RematchController` (overlay d'attente dans la scène de
jeu), qui n'avait jamais été câblé dans la scène et qui ne correspond pas à l'UX voulue.

## Contexte technique

- En WEB_BUILD, `NetworkManager`, `GameSessionManager` et `WebBootstrap` sont
  `DontDestroyOnLoad` : la connexion Colyseus et la session survivent à un changement de
  scène.
- Le serveur expose déjà : message `rematch` (→ `PlayerState.wantsRematch = true`), et
  quand les deux joueurs l'ont envoyé, `resetGameStateForRematch` repasse la room en
  `status = "loading"`.
- `LobbyPageUI` est dans la scène `MainMenu`. Elle est **déjà abonnée** à
  `GameSessionManager.OnGameStarted` (émis quand `status` passe à `"loading"`) et fait
  alors `LoadScene("main")`. Une nouvelle instance de `LobbyPageUI` est créée à chaque
  chargement de la scène `MainMenu` (son flag interne `_webBuildSceneLoadTriggered` est
  donc neuf).
- Le bug actuel : `GameOverUI.RestartGame()` cherche un `RematchController` absent de la
  scène → retombe sur un rechargement local de `main` → désync, compte à rebours bloqué.

## Architecture

### 1. Flag `RematchState`

Nouvelle classe statique `RematchState` avec un `bool ReturningForRematch` (faux par
défaut). Elle compile partout (référencée par du code non gardé `#if WEB_BUILD`).

### 2. Clic « Rejouer » — `GameOverUI.RestartGame()`

Dans le bloc `#if WEB_BUILD`, **hors mode DevSolo** :
- Envoie `rematch` au serveur : `NetworkManager.Instance?.SendRematch()`.
- Lève `RematchState.ReturningForRematch = true`.
- Charge la scène du menu : `SceneManager.LoadScene("MainMenu")`.

Le mode DevSolo garde le rechargement local (`GameManager.RestartGame()`). Le flux arcade
(`#else`) reste inchangé.

### 3. Scène menu — `MenuPageManager.Start()`

`MenuPageManager` est le **seul consommateur** du flag (pour éviter toute fragilité liée
à l'ordre des `OnEnable`). Dans `Start()` :
- Lire le flag dans une variable locale puis le remettre à faux immédiatement :
  `bool rematch = RematchState.ReturningForRematch; RematchState.ReturningForRematch = false;`
- Si `rematch` est vrai : activer le mode rematch sur la `LobbyPage`
  (`lobbyPage.GetComponent<LobbyPageUI>().EnterRematchMode()`) puis `ShowPage(lobbyPage)`.
- Sinon : comportement actuel (afficher `startingPage`/home).

### 4. `LobbyPageUI` — mode rematch-attente

`LobbyPageUI` expose une méthode publique `EnterRematchMode()` qui positionne un champ
d'instance `_rematchMode = true`. `MenuPageManager` l'appelle avant d'afficher la page.

Quand `_rematchMode` est vrai :
- L'affichage indique « En attente de l'autre joueur… ».
- Le bouton « Démarrer la partie » est **masqué** (`startGameButton.gameObject.SetActive(false)`)
  — le redémarrage est automatique, pas manuel.
- `OnGameStarted` → `LoadScene("main")` : comportement existant, inchangé. C'est ce qui
  relance la partie quand le serveur repasse en `"loading"`.
- Abonnement à `NetworkManager.OnPlayerRemoved` (`#if WEB_BUILD`) : si l'autre joueur
  quitte pendant l'attente → afficher « L'autre joueur a quitté la partie » et ne laisser
  que le bouton « Retour ».
- Le bouton « Retour » en mode rematch **annule et ramène au site** :
  `WebBridge.NotifyQuit(WebBootstrap.SessionId)` au lieu d'aller à la home.

En mode normal (premier démarrage), `LobbyPageUI` se comporte exactement comme aujourd'hui.

### 5. Serveur

Aucune modification. `handleRematch` + `resetGameStateForRematch` sont déjà en place.
Quand les deux clients ont envoyé `rematch`, la room repasse `status = "loading"` →
`OnGameStarted` fire sur les deux clients → les deux `LobbyPage` font `LoadScene("main")`.

## Flux complet

1. Joueur A clique « Rejouer » → `SendRematch()` + flag levé + `LoadScene("MainMenu")`.
2. Scène `MainMenu` chargée → `MenuPageManager` affiche la `LobbyPage` → mode rematch :
   « En attente de l'autre joueur… », pas de bouton Démarrer.
3. Joueur B clique « Rejouer » → même chose côté B.
4. Le serveur voit les deux `wantsRematch` → `resetGameStateForRematch` → `status="loading"`.
5. `OnGameStarted` fire sur A et B → les deux `LobbyPage` font `LoadScene("main")` → la
   partie redémarre sur la même session.

## Cas limite

- Si l'autre joueur quitte pendant l'attente : `OnPlayerRemoved` → message « L'autre
  joueur a quitté la partie », le joueur restant utilise « Retour » pour revenir au site.

## Hors périmètre

- Aucune modification serveur ni site Next.js.
- Pas de tests automatisés (Unity sans framework de tests — vérification manuelle à deux
  clients web).

## Fichiers

- Créer : `Assets/Scripts/Multiplayer/RematchState.cs` — flag statique.
- Modifier : `Assets/Scripts/GameOverUI.cs` — `RestartGame()` envoie `rematch` + va au lobby.
- Modifier : `Assets/Scripts/MenuPageManager.cs` — `Start()` affiche le lobby si rematch.
- Modifier : `Assets/Scripts/LobbyPageUI.cs` — mode rematch-attente.
- Supprimer : `Assets/Scripts/Multiplayer/RematchController.cs` — remplacé par ce flux.
