# Design — Raccourci dev « solo offline » en WEB_BUILD

Date : 2026-05-16
Repo : `momentum-game-v2` (client Unity) uniquement.

## Objectif

Permettre, **en développement uniquement**, de sauter directement dans une partie
solo de la build WEB_BUILD, sans serveur Colyseus, sans 2ᵉ client, sans lobby. Le
netcode est volontairement court-circuité : seule la scène/UI/inputs WEB_BUILD tourne.
Une vraie build WebGL livrée n'est jamais affectée.

## Contexte technique

Dans la build WEB_BUILD, en éditeur :
- `WebBootstrap.ReadParams()` lit des valeurs **par défaut** depuis `PlayerPrefs`
  (`DEBUG_SESSION_ID` → `"TEST-ROOM"`, `DEBUG_TOKEN` → `"tok-p1"`). Donc
  `WebBootstrap.IsReady` est **toujours `true` en éditeur** — on ne peut pas s'en
  servir comme signal « pas de partie réelle ».
- `NetworkManager` (auto-instancié) tente de se connecter à Colyseus dans `Start()`.
- `GameSessionManager.InitWebMode()` est appelé par `WebBootstrap` à chaque chargement
  de scène (puisque `IsReady` est vrai). Il attend ensuite un numéro de joueur envoyé
  par le serveur avant de configurer le joueur local (`SetupLocalPlayer`).
- `GameManager.Start()` lance `ServerDrivenCountdownCoroutine()` qui attend l'état du
  serveur — bloque indéfiniment hors-ligne.

## Le flag : `DevSolo`

Classe statique `DevSolo` avec un simple `bool Active` (faux par défaut). Elle compile
partout. Dans une build livrée, **rien ne la passe jamais à `true`** (le seul code qui
l'active est `#if UNITY_EDITOR`), donc tous les branchements `if (DevSolo.Active)` sont
du code mort inactif en production — pas besoin de les encadrer de `#if`.

## Le déclencheur : `DevSoloLauncher`

Composant `#if UNITY_EDITOR`, auto-instancié (`RuntimeInitializeOnLoadMethod`),
`DontDestroyOnLoad`, **sans aucun câblage de scène**. Deux déclencheurs :

1. **Touche F9 depuis le menu.** Dans `Update()`, si F9 est pressée et que la scène
   active n'est pas `main` → `DevSolo.Active = true` puis `SceneManager.LoadScene("main")`.

2. **Auto si on lance `main` directement.** Signal fiable : **la scène de boot est
   `main`** (le développeur a ouvert `main.unity` et pressé Play, au lieu de partir du
   menu). Au premier chargement de scène, si la scène active est `main` → `DevSolo.Active
   = true`. (On n'utilise PAS `!WebBootstrap.IsReady` : il est toujours vrai en éditeur.)

Contrainte de timing : le flag doit être posé avant que `GameSessionManager.InitWebMode()`
et `GameManager.Start()` de la scène `main` ne s'exécutent. Un éventuel essai de
connexion raté de `NetworkManager` avant que le flag soit posé est inoffensif (déjà
intercepté aujourd'hui). L'implémentation posera le flag via le callback
`SceneManager.sceneLoaded` / une initialisation précoce ; le détail exact relève du plan.

## Les 4 points de court-circuit

Tous des gardes `if (DevSolo.Active)`, dans les blocs `#if WEB_BUILD` existants :

1. **`NetworkManager.Start()`** — si `DevSolo.Active`, retour anticipé : aucune connexion
   Colyseus.

2. **`GameSessionManager.InitWebMode()`** — après la détection des GameObjects P1/P2,
   si `DevSolo.Active` : configurer directement P1 comme joueur local (activer P1,
   désactiver P2, caméra plein écran) **sans** ajouter `LocalPlayerSync` et **sans**
   s'abonner aux événements réseau, puis retour anticipé. Concrètement, `SetupLocalPlayer`
   gagne un paramètre optionnel pour ne pas attacher `LocalPlayerSync`, et la branche
   DevSolo l'appelle avec `playerNumber = 1`.

3. **`GameManager.Start()`** — dans le bloc `#if WEB_BUILD`, si `DevSolo.Active` lancer
   `StartCountdownCoroutine()` (le compte à rebours local, déjà compilé sans condition,
   sans réseau) au lieu de `ServerDrivenCountdownCoroutine()`.

4. **`GameOverUI.RestartGame()`** — dans le bloc `#if WEB_BUILD`, si `DevSolo.Active`,
   ne pas router vers `RematchController` (qui resterait bloqué hors-ligne) : faire un
   simple `GameManager.Instance.RestartGame()` local.

## Résultat attendu

En éditeur avec le define `WEB_BUILD` :
- Lancer le menu en Play puis presser **F9** → saut immédiat dans `main` en solo.
- OU ouvrir `main.unity` et presser **Play** → solo automatiquement.

Dans les deux cas : on contrôle P1, P2 est désactivé, caméra plein écran, compte à
rebours local, aucun serveur requis. Les scores ne sont pas persistés (`sessionId`
absent → `SendScores` ignore l'envoi, déjà géré). « Rejouer » recharge la scène en local.

## Hors périmètre

- Aucune modification du serveur Colyseus ni de l'API Next.js.
- Pas de mode solo en production / build livrée.
- Pas de tests automatisés (Unity sans framework de tests — vérification manuelle :
  presser F9 / lancer `main`, jouer, vérifier l'absence d'erreurs console liées à un
  serveur ou une `Room` manquante).

## Fichiers

- Créer : `Assets/Scripts/Dev/DevSolo.cs` — flag statique.
- Créer : `Assets/Scripts/Dev/DevSoloLauncher.cs` — `#if UNITY_EDITOR`, déclencheurs.
- Modifier : `Assets/Scripts/Multiplayer/NetworkManager.cs` — garde dans `Start()`.
- Modifier : `Assets/Scripts/GameSessionManager.cs` — branche DevSolo dans `InitWebMode()`
  + paramètre optionnel sur `SetupLocalPlayer`.
- Modifier : `Assets/Scripts/GameManager.cs` — branche DevSolo dans `Start()`.
- Modifier : `Assets/Scripts/GameOverUI.cs` — branche DevSolo dans `RestartGame()`.
