# Design — Menu, contrôles & rejouer (Momentum WEB_BUILD)

Date : 2026-05-16
Plateforme cible : **WEB_BUILD uniquement** (jeu joué dans le navigateur via iframe). Le flux
arcade (`!WEB_BUILD`) reste inchangé pour les 4 fonctionnalités.

Repos concernés :
- `momentum-game-v2` (client Unity) — gros du travail
- `momentum-server` (serveur Colyseus) — Feature 4
- `momentum` (site Next.js) — vérification d'une route, Feature 4

Les 4 fonctionnalités sont indépendantes les unes des autres et peuvent être implémentées
et testées séparément.

---

## Feature 1 — Persistance de la session lors de la navigation menu

### Problème
Flux : menu → « Démarrer » → lobby → « Retour » → home → « Comment jouer » → retour →
« Démarrer ». La session en cours est perdue.

### Cause racine
`LobbyPageUI.OnBackButtonClicked()` (`Assets/Scripts/LobbyPageUI.cs:219`) appelle
`GameSessionManager.ResetSession()` (`GameSessionManager.cs:924`), qui met `sessionId = null`
et efface `player1Pseudo`/`player2Pseudo`/`bothPlayersReady`.

En WEB_BUILD la session vient de l'URL (`WebBootstrap.SessionId`) et la connexion Colyseus
est persistante (`NetworkManager` est `DontDestroyOnLoad`). Le menu ne « possède » pas la
session — il ne devrait jamais la détruire. Après reset, re-cliquer « Démarrer » fait que
`CreateGameSession()` ne voit plus de `sessionId` et crée une **session orpheline** via
`/api/game/session`.

### Solution
Dans `LobbyPageUI.OnBackButtonClicked()`, sous `#if WEB_BUILD` : ne plus appeler
`ResetSession()`. Naviguer simplement vers la home (`MenuPageManager.ShowHomePage()`).

La session (`sessionId`, room Colyseus, pseudos, `bothPlayersReady`) survit. Re-cliquer
« Démarrer » → `CreateGameSession()` reste idempotent en web (skip si `sessionId` présent) →
`ShowQRCodePage()` redirige vers le lobby → `LobbyPageUI.OnEnable()` → `UpdateDisplay()`
réaffiche les pseudos toujours présents.

Le bloc `#else` (arcade) garde `ResetSession()` + `ShowQRCodePage()`.

### Fichiers
- `Assets/Scripts/LobbyPageUI.cs` — modifier `OnBackButtonClicked()` (~5 lignes)

---

## Feature 2 — Détection clavier/manette + affichage des touches

### Détection
Nouveau composant `InputDeviceDetector` (singleton, `DontDestroyOnLoad`). Contrainte :
ancien Input Manager d'Unity (pas de package Input System).

- Manette : `Input.GetJoystickNames()` non vide **et/ou** activité détectée sur les axes
  joystick (`P1_Horizontal` au-delà d'un seuil, `GetKey` sur `JoystickButton0..N`).
- Clavier : `Input.anyKeyDown` sur une `KeyCode` clavier.
- Garde un `CurrentDevice` (`Keyboard` | `Gamepad`) = dernier device ayant produit une
  activité. Émet `OnDeviceChanged` au changement.

En WebGL, le navigateur n'expose une manette qu'après une première pression de bouton —
le détecteur réagit donc à la première activité manette. Device par défaut : `Keyboard`.

### Définition centrale des contrôles
Un type `ControlScheme` (statique ou ScriptableObject) décrit, par action
(gauche, droite, saut, glissade, lumière), un libellé d'affichage :
- Pour le clavier : libellé dérivé du binding personnalisé (Feature 3).
- Pour la manette : libellé/glyphe générique fixe (stick, A, B, X).

C'est l'unique source de vérité partagée entre le bandeau en jeu et la page « Comment jouer ».

### Affichage
- `ControlHintsBar` : bandeau discret en bas de l'écran de jeu, listant les touches du
  device courant. S'abonne à `InputDeviceDetector.OnDeviceChanged` pour se rafraîchir.
- Page « Comment jouer » (`HowToPlayPageUI`) : une section de contrôles qui affiche le même
  contenu, mis à jour selon le device détecté.

### Fichiers
- `Assets/Scripts/Input/InputDeviceDetector.cs` (nouveau)
- `Assets/Scripts/Input/ControlScheme.cs` (nouveau)
- `Assets/Scripts/UI/ControlHintsBar.cs` (nouveau)
- `Assets/Scripts/HowToPlayPageUI.cs` — ajout d'une section contrôles
- Prefabs/UI à câbler dans la scène de jeu et la page « Comment jouer »

---

## Feature 3 — Personnalisation des touches (clavier)

### Contrainte
L'ancien Input Manager ne permet pas le rebinding à l'exécution → couche custom légère.
Pas de migration vers le nouveau Input System. **Clavier uniquement** — la manette reste
sur les axes fixes de l'Input Manager.

### Modèle
`KeyboardControls` : un jeu de `KeyCode` par action (gauche, droite, saut, glissade,
lumière), persisté en `PlayerPrefs` (clés `kb_left`, `kb_right`, `kb_jump`, `kb_slide`,
`kb_light`), avec valeurs par défaut. En web il y a **un seul joueur local par navigateur**,
donc un seul jeu de bindings par machine — personnalisé par la personne qui joue sur cette
machine.

### Intégration à PlayerInput
En `#if WEB_BUILD`, `PlayerInput.Update()` lit **deux sources** et les combine :
- Axes Input Manager (`P1_Horizontal`, `P1_B1`…) → manette, inchangé.
- `KeyCode` rebindables via `Input.GetKey` / `GetKeyDown` → clavier.

Combinaison : pour l'horizontale, `keyboardValue` (−1/0/+1 calculé depuis les `KeyCode`
gauche/droite) prioritaire s'il est non nul, sinon valeur de l'axe. Pour saut/glissade/
lumière, OR des deux sources. La manette continue de fonctionner, le clavier devient
personnalisable. Le flux arcade lit les axes comme aujourd'hui.

### UI de rebinding
`ControlsSettingsUI` : écran accessible depuis le menu. Par action : libellé de la touche
actuelle + bouton « Réassigner » → passe en mode capture → la prochaine `KeyCode` pressée
est sauvegardée (rejet des touches réservées si besoin). Bouton « Réinitialiser » qui
restaure les valeurs par défaut.

Accès : un bouton « Contrôles » sur la page d'accueil ou « Comment jouer » (à câbler).

### Fichiers
- `Assets/Scripts/Input/KeyboardControls.cs` (nouveau)
- `Assets/Scripts/UI/ControlsSettingsUI.cs` (nouveau)
- `Assets/Scripts/PlayerScripts/PlayerInput.cs` — lecture combinée en WEB_BUILD
- Prefab/page UI de réglages des contrôles + bouton d'accès

---

## Feature 4 — Rejouer (rematch) synchronisé sur la même session

### Problème
Le bouton « Rejouer » appelle `GameManager.RestartGame()` (`GameManager.cs:751`) qui ne
fait qu'un `SceneManager.LoadScene` local. En WEB_BUILD : le serveur reste à
`status = "finished"`, l'autre joueur ne redémarre pas → « il ne se passe rien ».

### Serveur (`momentum-server`)
`src/schema/PlayerState.ts` : ajout d'un champ `wantsRematch: boolean = false` (+ entrée
`defineTypes`).

`src/rooms/MomentumRoom.ts` :
- Handler `onMessage("rematch", client => …)` : passe `player.wantsRematch = true` pour ce
  client. N'agit que si `status === "finished"`.
- Quand **tous** les joueurs ont `wantsRematch === true` → `resetForRematch()` :
  - Chaque `PlayerState` : `isAlive = true`, `hasFinished = false`, `score = 0`,
    `distanceTraveled/survivalTime/collectibles = 0`, `wantsRematch = false`, positions/
    vélocités/`actionSeq` remis à 0, `isStunned = false`.
  - `GameState` : `winnerSessionId = ""`, `elapsedTime = 0`, `countdownRemaining = 0`.
  - Vider `sceneReadySessionIds`.
  - `status = "loading"` → relance le handshake existant
    `loading → sceneReady → countdown → playing`.

### Schéma C# généré (`momentum-game-v2`)
`Assets/Scripts/Multiplayer/Schema/PlayerState.cs` est auto-généré. Ajouter le champ
`wantsRematch` avec l'index `[Type]` suivant, en miroir du schéma serveur (l'ordre des
champs doit correspondre).

### Client Unity (`momentum-game-v2`, WEB_BUILD)
- `NetworkManager.SendRematch()` → `Room?.Send("rematch")`.
- Bouton « Rejouer » du `GameOverPanel` : en WEB_BUILD, n'appelle plus le reload local.
  Il envoie `rematch` et bascule le panel en état « En attente de l'autre joueur… ».
- La scène de jeu écoute le state serveur : `status` repasse à `"loading"` →
  `SceneManager.LoadScene("main")`. Réutilise l'event `OnGameStarted` déjà émis par
  `GameSessionManager.SetupGameStateListener()` sur `status == "loading"` ; un petit
  composant de la scène de jeu s'y abonne et charge la scène (le `LobbyPageUI` qui fait
  ça aujourd'hui n'est présent que dans la scène menu).
- **Abandon (annulation immédiate) :** pendant l'état « En attente… », s'abonner à
  `NetworkManager.OnPlayerRemoved`. Si l'autre joueur quitte → afficher « L'autre joueur a
  quitté la partie » et ne laisser que le bouton « Quitter » (retour au site via
  `WebBridge`). Aucun message serveur supplémentaire nécessaire — Colyseus émet déjà
  `OnRemove` au départ d'un joueur.
- Flux arcade (`!WEB_BUILD`) : conserve le reload local actuel de `GameManager.RestartGame()`.

### Persistance des scores — à vérifier
À chaque fin de partie (y compris après rematch), les clients re-POSTent vers
`/api/game/end`. `markGameSessionPlaying` (appelé au `startGame` du serveur) repasse la
`GameSession` DB en `playing`, donc le endpoint réaccepte les scores après un rematch.

**Point à auditer dans le repo `momentum` :** la route `/api/game/end` doit faire un
*upsert* des scores (une ligne par session+joueur) et non un *insert*, sinon le classement
afficherait des doublons après chaque rematch. Si elle insère, la corriger en upsert.

### Fichiers
- `momentum-server/src/schema/PlayerState.ts` — champ `wantsRematch`
- `momentum-server/src/rooms/MomentumRoom.ts` — handler `rematch` + `resetForRematch()`
- `momentum-game-v2/Assets/Scripts/Multiplayer/Schema/PlayerState.cs` — champ miroir
- `momentum-game-v2/Assets/Scripts/Multiplayer/NetworkManager.cs` — `SendRematch()`
- `momentum-game-v2/Assets/Scripts/GameManager.cs` et/ou `GameOverUI.cs` — bouton Rejouer
  WEB_BUILD
- `momentum-game-v2` — composant de la scène de jeu qui gère le rematch (envoi, état
  d'attente, reload sur `status=loading`, abandon)
- `momentum/src/app/api/game/end/route.ts` — vérifier/corriger l'upsert
