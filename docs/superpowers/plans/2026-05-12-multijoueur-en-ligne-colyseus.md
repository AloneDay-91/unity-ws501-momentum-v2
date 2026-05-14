# Multijoueur en Ligne (Web) via Colyseus — Plan d'Implémentation

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Permettre à 2 joueurs de jouer ensemble à Momentum à distance via le navigateur, depuis le site Next.js existant, sans casser le mode arcade local actuel.

**Architecture:** Ajouter une couche temps réel **Colyseus (Node.js + TypeScript)** déployée à côté du Next.js sur le même VPS. Unity WebGL (mode online) se connecte au game server en WebSocket, qui valide l'auth via les `GameSession` existantes en DB (réutilise Prisma + Better Auth tokens). Le code Unity utilise des **scripting define symbols** (`ARCADE_BUILD` vs `WEB_BUILD`) pour conserver le mode arcade local intact. Modèle réseau **client-authoritative** avec interpolation pour le joueur distant (acceptable pour ce platformer non-compétitif).

**Tech Stack:**
- Game server : Colyseus 0.15.x, TypeScript, Node 20, `@colyseus/schema`, `mysql2` (réutilise la DB existante)
- Unity client : `colyseus-unity-sdk` (officiel), Unity 2022.3.62f1
- Site web : Next.js 15 + Better Auth + Prisma (existant)
- DB : MySQL (existant, table `game_session` déjà présente)
- Déploiement : VPS existant (51.178.85.130), reverse proxy nginx, PM2 pour le process Node

**Note sur le découpage** : ce plan couvre 3 sous-systèmes (game server, refactor Unity, lobby web). Il est structuré en **3 milestones**, chaque milestone produisant du software testable indépendamment. Si tu préfères, on peut découper en 3 plans séparés à la fin du Milestone 1.

---

## File Structure

### Nouveau projet : `momentum-server` (Colyseus, à côté du site)
```
/Users/elouan/buts4/www/html/SAE501/momentum-server/
├── package.json
├── tsconfig.json
├── .env
├── src/
│   ├── index.ts                 # Entry point, démarre le serveur Colyseus
│   ├── rooms/
│   │   └── MomentumRoom.ts      # Logique room 2-joueurs
│   ├── schema/
│   │   ├── GameState.ts         # State racine de la room
│   │   └── PlayerState.ts       # State par joueur (pos, vel, anim, score)
│   ├── auth/
│   │   └── verifyGameSession.ts # Bridge auth : valide tokens contre DB
│   ├── db/
│   │   └── prisma.ts            # Client Prisma (réutilise schema du site)
│   └── config.ts                # Constantes (tickrate, port, etc.)
└── tests/
    ├── room.test.ts
    └── auth.test.ts
```

### Modifications site Next.js : `/Users/elouan/buts4/www/html/SAE501/momentum/`
```
src/app/api/lobby/
└── create/route.ts              # NOUVEAU — crée GameSession + room Colyseus
src/app/lobby/
├── page.tsx                     # NOUVEAU — UI création/join lobby
└── [sessionId]/page.tsx         # NOUVEAU — page d'attente (waiting room)
src/app/play/
└── [sessionId]/page.tsx         # NOUVEAU — embed du build WebGL
src/lib/
└── colyseus-admin.ts            # NOUVEAU — appels admin vers Colyseus
```

### Modifications projet Unity : `/Users/elouan/Desktop/WS501D/momentum-game-v2/`
```
Assets/Scripts/Multiplayer/
├── InterferenceSystem.cs        # MODIFIÉ — RPC réseau en mode WEB_BUILD
├── NetworkManager.cs            # NOUVEAU — singleton connexion Colyseus
├── NetworkPlayer.cs             # NOUVEAU — joueur distant (interpolation)
├── LocalPlayerSync.cs           # NOUVEAU — envoie state local au serveur
├── WebBootstrap.cs              # NOUVEAU — lit URL params (sessionId, token)
└── Schema/                      # Généré par colyseus-unity-sdk depuis le serveur
    ├── GameState.cs
    └── PlayerState.cs

Assets/Scripts/PlayerScripts/
├── PlayerInput.cs               # MODIFIÉ — playerID forcé à 1 en WEB_BUILD
└── PlayerMovement.cs            # MODIFIÉ — désactivable pour NetworkPlayer

Assets/Scripts/GameSessionManager.cs   # MODIFIÉ — bypass split-screen en WEB_BUILD
Assets/Scripts/SplitScreenManager.cs   # MODIFIÉ — désactivé via #if !WEB_BUILD
Assets/Plugins/Colyseus/                # NOUVEAU — SDK importé via .unitypackage

ProjectSettings/ProjectSettings.asset   # MODIFIÉ — ajoute defines ARCADE_BUILD/WEB_BUILD
```

---

## Milestone 1 : Game Server Colyseus Standalone

**Objectif** : un serveur Colyseus testable en isolation qui accepte 2 joueurs, synchronise leurs positions, et valide leur identité contre la DB MySQL existante. Aucune modif Unity ni Next.js dans ce milestone.

**Critère de succès** : avec 2 onglets de l'inspecteur Colyseus (`@colyseus/playground`), je peux join une room avec 2 fake clients, voir leurs positions sync, et le 3e client est rejeté.

### Task 1.1 : Initialiser le projet Colyseus

**Files:**
- Create: `/Users/elouan/buts4/www/html/SAE501/momentum-server/package.json`
- Create: `/Users/elouan/buts4/www/html/SAE501/momentum-server/tsconfig.json`
- Create: `/Users/elouan/buts4/www/html/SAE501/momentum-server/.env`
- Create: `/Users/elouan/buts4/www/html/SAE501/momentum-server/.gitignore`
- Create: `/Users/elouan/buts4/www/html/SAE501/momentum-server/src/index.ts`

- [ ] **Step 1 : Créer le dossier et initialiser**

```bash
mkdir -p /Users/elouan/buts4/www/html/SAE501/momentum-server
cd /Users/elouan/buts4/www/html/SAE501/momentum-server
npm init -y
npm install colyseus @colyseus/schema @colyseus/monitor @colyseus/playground express dotenv
npm install -D typescript @types/node @types/express tsx vitest
```

- [ ] **Step 2 : Configurer TypeScript**

Créer `tsconfig.json` :
```json
{
  "compilerOptions": {
    "target": "ES2022",
    "module": "commonjs",
    "moduleResolution": "node",
    "esModuleInterop": true,
    "strict": true,
    "experimentalDecorators": true,
    "emitDecoratorMetadata": true,
    "outDir": "dist",
    "rootDir": "src",
    "skipLibCheck": true,
    "resolveJsonModule": true
  },
  "include": ["src/**/*"]
}
```

- [ ] **Step 3 : Créer `.env`**

```env
PORT=2567
DATABASE_URL="mysql://sae501user:password@51.178.85.130:3306/sae501"
NEXT_API_URL="http://localhost:3000"
GAME_SERVER_SECRET="change-me-random-32-bytes"
```

⚠️ Récupère `DATABASE_URL` depuis `/Users/elouan/buts4/www/html/SAE501/momentum/.env` — utilise EXACTEMENT la même valeur. Génère `GAME_SERVER_SECRET` avec `openssl rand -base64 32`.

- [ ] **Step 4 : Créer `.gitignore`**

```
node_modules/
dist/
.env
*.log
```

- [ ] **Step 5 : Mettre à jour `package.json` scripts**

Modifier la section `scripts` :
```json
"scripts": {
  "dev": "tsx watch src/index.ts",
  "build": "tsc",
  "start": "node dist/index.ts",
  "test": "vitest run"
}
```

- [ ] **Step 6 : Créer le serveur minimal `src/index.ts`**

```typescript
import "dotenv/config";
import { Server } from "colyseus";
import { WebSocketTransport } from "@colyseus/ws-transport";
import { monitor } from "@colyseus/monitor";
import { playground } from "@colyseus/playground";
import express from "express";
import { createServer } from "http";

const port = Number(process.env.PORT || 2567);
const app = express();
const httpServer = createServer(app);

const gameServer = new Server({
  transport: new WebSocketTransport({ server: httpServer }),
});

app.use("/colyseus", monitor());
app.use("/playground", playground);
app.get("/health", (_, res) => res.json({ ok: true }));

httpServer.listen(port, () => {
  console.log(`[Momentum] Game server listening on :${port}`);
});
```

- [ ] **Step 7 : Vérifier que ça tourne**

```bash
cd /Users/elouan/buts4/www/html/SAE501/momentum-server
npm run dev
```
Expected : `[Momentum] Game server listening on :2567`. Vérifier `http://localhost:2567/health` → `{"ok":true}`. Stop avec Ctrl+C.

- [ ] **Step 8 : Commit**

```bash
cd /Users/elouan/buts4/www/html/SAE501/momentum-server
git init
git add .
git commit -m "feat(server): initial Colyseus server skeleton"
```

---

### Task 1.2 : Définir le State partagé (GameState + PlayerState)

**Files:**
- Create: `momentum-server/src/schema/PlayerState.ts`
- Create: `momentum-server/src/schema/GameState.ts`

- [ ] **Step 1 : Créer `PlayerState.ts`**

```typescript
import { Schema, type } from "@colyseus/schema";

export class PlayerState extends Schema {
  @type("number") playerNumber: number = 0;        // 1 ou 2
  @type("string") pseudo: string = "";

  // Transform
  @type("number") posX: number = 0;
  @type("number") posY: number = 0;
  @type("number") posZ: number = 0;
  @type("number") velX: number = 0;
  @type("number") velY: number = 0;
  @type("number") velZ: number = 0;
  @type("number") rotY: number = 0;

  // Animation flags
  @type("boolean") isGrounded: boolean = false;
  @type("boolean") isSliding: boolean = false;
  @type("boolean") isStunned: boolean = false;
  @type("number") horizontalInput: number = 0;     // pour anim run/idle côté distant

  // Game state
  @type("number") score: number = 0;
  @type("number") distanceTraveled: number = 0;
  @type("number") survivalTime: number = 0;
  @type("number") collectibles: number = 0;
  @type("boolean") hasFinished: boolean = false;
  @type("boolean") isAlive: boolean = true;
}
```

- [ ] **Step 2 : Créer `GameState.ts`**

```typescript
import { Schema, type, MapSchema } from "@colyseus/schema";
import { PlayerState } from "./PlayerState";

export class GameState extends Schema {
  @type({ map: PlayerState }) players = new MapSchema<PlayerState>();
  @type("string") status: string = "waiting";       // waiting | countdown | playing | finished
  @type("number") countdownRemaining: number = 0;
  @type("number") elapsedTime: number = 0;
  @type("string") winnerSessionId: string = "";
  @type("string") mapName: string = "main";
}
```

- [ ] **Step 3 : Commit**

```bash
git add src/schema
git commit -m "feat(server): add GameState and PlayerState schema"
```

---

### Task 1.3 : Créer la `MomentumRoom` (logique 2-joueurs)

**Files:**
- Create: `momentum-server/src/config.ts`
- Create: `momentum-server/src/rooms/MomentumRoom.ts`
- Modify: `momentum-server/src/index.ts`

- [ ] **Step 1 : Créer `config.ts`**

```typescript
export const TICK_RATE_HZ = 20;              // 20 updates/sec serveur → clients
export const MAX_CLIENTS_PER_ROOM = 2;
export const COUNTDOWN_SECONDS = 3;
export const ROOM_TIMEOUT_MS = 5 * 60 * 1000; // 5 min sans activité = ferme
```

- [ ] **Step 2 : Créer `MomentumRoom.ts`**

```typescript
import { Room, Client } from "colyseus";
import { GameState } from "../schema/GameState";
import { PlayerState } from "../schema/PlayerState";
import {
  TICK_RATE_HZ,
  MAX_CLIENTS_PER_ROOM,
  COUNTDOWN_SECONDS,
} from "../config";

interface JoinOptions {
  sessionId: string;       // GameSession.sessionId (DB)
  token: string;           // player1Token ou player2Token
  pseudo?: string;
}

interface PlayerInputMessage {
  posX: number; posY: number; posZ: number;
  velX: number; velY: number; velZ: number;
  rotY: number;
  isGrounded: boolean;
  isSliding: boolean;
  horizontalInput: number;
}

export class MomentumRoom extends Room<GameState> {
  maxClients = MAX_CLIENTS_PER_ROOM;
  patchRate = 1000 / TICK_RATE_HZ;
  autoDispose = true;

  // Mapping client.sessionId → playerNumber (1 ou 2)
  private playerNumbers = new Map<string, number>();

  onCreate(options: { gameSessionId: string }) {
    this.setState(new GameState());
    this.setMetadata({ gameSessionId: options.gameSessionId });

    // Auth de chaque join — implémenté en Task 1.4
    this.onMessage("input", (client, msg: PlayerInputMessage) =>
      this.handleInput(client, msg)
    );
    this.onMessage("stun", (client) => this.handleStun(client));
    this.onMessage("finish", (client, payload: { score: number }) =>
      this.handleFinish(client, payload)
    );

    console.log(`[Room] Created for gameSession=${options.gameSessionId}`);
  }

  // Stub d'auth — Task 1.4 va le remplacer
  async onAuth(client: Client, options: JoinOptions): Promise<{ playerNumber: number; pseudo: string }> {
    if (!options.token) throw new Error("Missing token");
    // Pour l'instant, accepte n'importe quoi — la vraie auth arrive Task 1.4
    const playerNumber = this.state.players.size + 1;
    return { playerNumber, pseudo: options.pseudo ?? `Player${playerNumber}` };
  }

  onJoin(client: Client, _options: JoinOptions, auth: { playerNumber: number; pseudo: string }) {
    const player = new PlayerState();
    player.playerNumber = auth.playerNumber;
    player.pseudo = auth.pseudo;
    this.state.players.set(client.sessionId, player);
    this.playerNumbers.set(client.sessionId, auth.playerNumber);

    console.log(`[Room] ${auth.pseudo} (P${auth.playerNumber}) joined`);

    if (this.state.players.size === MAX_CLIENTS_PER_ROOM) {
      this.startCountdown();
    }
  }

  onLeave(client: Client, _consented: boolean) {
    this.state.players.delete(client.sessionId);
    this.playerNumbers.delete(client.sessionId);
    console.log(`[Room] Client ${client.sessionId} left`);

    if (this.state.status === "playing") {
      // Si l'autre joueur part en cours de jeu, on termine
      this.state.status = "finished";
    }
  }

  onDispose() {
    console.log(`[Room] Disposed`);
  }

  // === Game flow ===

  private startCountdown() {
    this.state.status = "countdown";
    this.state.countdownRemaining = COUNTDOWN_SECONDS;
    const interval = this.clock.setInterval(() => {
      this.state.countdownRemaining -= 1;
      if (this.state.countdownRemaining <= 0) {
        interval.clear();
        this.startGame();
      }
    }, 1000);
  }

  private startGame() {
    this.state.status = "playing";
    this.state.elapsedTime = 0;
    this.clock.setInterval(() => {
      if (this.state.status === "playing") this.state.elapsedTime += 0.1;
    }, 100);
  }

  // === Message handlers ===

  private handleInput(client: Client, msg: PlayerInputMessage) {
    const player = this.state.players.get(client.sessionId);
    if (!player || this.state.status !== "playing" || player.isStunned) return;

    // Client-authoritative : on accepte la position telle quelle
    // (validation anti-cheat hors scope du POC)
    player.posX = msg.posX;
    player.posY = msg.posY;
    player.posZ = msg.posZ;
    player.velX = msg.velX;
    player.velY = msg.velY;
    player.velZ = msg.velZ;
    player.rotY = msg.rotY;
    player.isGrounded = msg.isGrounded;
    player.isSliding = msg.isSliding;
    player.horizontalInput = msg.horizontalInput;
  }

  private handleStun(attacker: Client) {
    const attackerPlayer = this.state.players.get(attacker.sessionId);
    if (!attackerPlayer || this.state.status !== "playing") return;

    // Stun l'autre joueur
    this.state.players.forEach((player, sessionId) => {
      if (sessionId !== attacker.sessionId && !player.isStunned) {
        player.isStunned = true;
        this.clock.setTimeout(() => {
          player.isStunned = false;
        }, 1000); // doit matcher InterferenceSystem.stunDuration
      }
    });
  }

  private handleFinish(client: Client, payload: { score: number }) {
    const player = this.state.players.get(client.sessionId);
    if (!player || player.hasFinished) return;
    player.hasFinished = true;
    player.score = payload.score;

    // Si tous ont fini ou si un seul reste, finir la partie
    let allFinished = true;
    this.state.players.forEach((p) => { if (!p.hasFinished) allFinished = false; });
    if (allFinished) {
      this.state.status = "finished";
      this.determineWinner();
    }
  }

  private determineWinner() {
    let bestScore = -1;
    let winnerSessionId = "";
    this.state.players.forEach((p, sessionId) => {
      if (p.score > bestScore) {
        bestScore = p.score;
        winnerSessionId = sessionId;
      }
    });
    this.state.winnerSessionId = winnerSessionId;
  }
}
```

- [ ] **Step 3 : Enregistrer la room dans `index.ts`**

Modifier `src/index.ts`, ajouter après la création de `gameServer` :
```typescript
import { MomentumRoom } from "./rooms/MomentumRoom";

gameServer.define("momentum", MomentumRoom);
```

- [ ] **Step 4 : Tester avec le playground**

```bash
npm run dev
```
Ouvrir `http://localhost:2567/playground`. Cliquer sur "momentum" → "Join". Vérifier que le state apparaît. Ouvrir un 2e onglet, join → countdown démarre. Ouvrir un 3e → rejeté (room pleine).

- [ ] **Step 5 : Commit**

```bash
git add .
git commit -m "feat(server): add MomentumRoom with 2-player flow"
```

---

### Task 1.4 : Auth bridge — valider via `GameSession` en DB

**Files:**
- Create: `momentum-server/src/db/prisma.ts`
- Create: `momentum-server/src/auth/verifyGameSession.ts`
- Modify: `momentum-server/src/rooms/MomentumRoom.ts` (méthode `onAuth`)

⚠️ **Décision** : on **réutilise le client Prisma** du site Next.js plutôt que de générer un client séparé. C'est plus simple et garantit la cohérence du schema.

- [ ] **Step 1 : Installer Prisma dans le serveur**

```bash
cd /Users/elouan/buts4/www/html/SAE501/momentum-server
npm install @prisma/client
npm install -D prisma
```

- [ ] **Step 2 : Copier le schema Prisma**

```bash
mkdir -p prisma
cp /Users/elouan/buts4/www/html/SAE501/momentum/prisma/schema.prisma prisma/schema.prisma
npx prisma generate
```

⚠️ **À noter** : si le schema du site change, il faut re-copier ce fichier. Alternative : importer le `@prisma/client` du site directement via `file:../momentum/node_modules/@prisma/client` dans `package.json` — décide selon ta préférence.

- [ ] **Step 3 : Créer `db/prisma.ts`**

```typescript
import { PrismaClient } from "@prisma/client";

declare global {
  // eslint-disable-next-line no-var
  var __prisma: PrismaClient | undefined;
}

export const prisma = global.__prisma ?? new PrismaClient();
if (process.env.NODE_ENV !== "production") global.__prisma = prisma;
```

- [ ] **Step 4 : Créer `auth/verifyGameSession.ts`**

```typescript
import { prisma } from "../db/prisma";

export interface VerifyResult {
  ok: true;
  playerNumber: 1 | 2;
  pseudo: string;
  gameSessionInternalId: string; // GameSession.id (cuid)
}

export interface VerifyError {
  ok: false;
  reason: string;
}

export async function verifyGameSession(
  sessionId: string,
  token: string
): Promise<VerifyResult | VerifyError> {
  if (!sessionId || !token) {
    return { ok: false, reason: "missing-credentials" };
  }

  const session = await prisma.gameSession.findUnique({
    where: { sessionId },
  });

  if (!session) return { ok: false, reason: "session-not-found" };
  if (session.expiresAt < new Date()) return { ok: false, reason: "expired" };
  if (session.status === "finished") return { ok: false, reason: "session-finished" };

  let playerNumber: 1 | 2;
  let pseudo: string;
  if (token === session.player1Token) {
    playerNumber = 1;
    pseudo = session.player1Pseudo ?? "Player1";
  } else if (token === session.player2Token) {
    playerNumber = 2;
    pseudo = session.player2Pseudo ?? "Player2";
  } else {
    return { ok: false, reason: "invalid-token" };
  }

  return {
    ok: true,
    playerNumber,
    pseudo,
    gameSessionInternalId: session.id,
  };
}

export async function markPlayerJoined(
  gameSessionInternalId: string,
  playerNumber: 1 | 2
): Promise<void> {
  await prisma.gameSession.update({
    where: { id: gameSessionInternalId },
    data: playerNumber === 1
      ? { player1Joined: true }
      : { player2Joined: true },
  });
}
```

- [ ] **Step 5 : Brancher l'auth dans `MomentumRoom.onAuth`**

Remplacer `onAuth` dans `src/rooms/MomentumRoom.ts` :
```typescript
import { verifyGameSession, markPlayerJoined } from "../auth/verifyGameSession";

// ...

async onAuth(client: Client, options: JoinOptions) {
  const result = await verifyGameSession(options.sessionId, options.token);
  if (!result.ok) {
    throw new Error(`Auth failed: ${result.reason}`);
  }

  // Vérifier que ce playerNumber n'est pas déjà pris dans la room
  let alreadyTaken = false;
  this.state.players.forEach((p) => {
    if (p.playerNumber === result.playerNumber) alreadyTaken = true;
  });
  if (alreadyTaken) throw new Error("player-slot-taken");

  await markPlayerJoined(result.gameSessionInternalId, result.playerNumber);

  return {
    playerNumber: result.playerNumber,
    pseudo: result.pseudo,
    gameSessionInternalId: result.gameSessionInternalId,
  };
}
```

⚠️ Met à jour `onJoin` pour stocker `gameSessionInternalId` côté room (utile pour la persistence finale en Task 1.6) :
```typescript
private gameSessionInternalId: string = "";

onJoin(client: Client, _options: JoinOptions, auth: { playerNumber: number; pseudo: string; gameSessionInternalId: string }) {
  this.gameSessionInternalId = auth.gameSessionInternalId;
  // ... reste inchangé
}
```

- [ ] **Step 6 : Test manuel avec une vraie GameSession**

Insère manuellement une row de test en DB (via mysql client ou Prisma Studio dans le site Next.js) :
```sql
INSERT INTO game_session (id, sessionId, player1Token, player2Token, status, expiresAt, createdAt)
VALUES ('test-cuid', 'TEST-ROOM', 'token-p1', 'token-p2', 'waiting',
        DATE_ADD(NOW(), INTERVAL 1 HOUR), NOW());
```

Ouvrir `http://localhost:2567/playground`, créer une room "momentum" avec options :
```json
{ "gameSessionId": "test-cuid" }
```
Puis joindre avec `{ "sessionId": "TEST-ROOM", "token": "token-p1" }` → succès.
Tenter `{ "sessionId": "TEST-ROOM", "token": "wrong" }` → rejet `Auth failed: invalid-token`.

- [ ] **Step 7 : Commit**

```bash
git add .
git commit -m "feat(server): authenticate joins via GameSession in MySQL"
```

---

### Task 1.5 : Tests unitaires de l'auth

**Files:**
- Create: `momentum-server/tests/auth.test.ts`

⚠️ Les tests utilisent une DB MySQL réelle (la même que l'app). Il faut isoler avec un préfixe `TEST-` sur les sessionIds. Pas de mocks Prisma — on a besoin de tester le contrat avec MySQL pour de vrai.

- [ ] **Step 1 : Créer `tests/auth.test.ts`**

```typescript
import { describe, it, expect, beforeAll, afterAll } from "vitest";
import { prisma } from "../src/db/prisma";
import { verifyGameSession } from "../src/auth/verifyGameSession";

const TEST_SESSION_ID = "TEST-AUTH-" + Date.now();

beforeAll(async () => {
  await prisma.gameSession.create({
    data: {
      sessionId: TEST_SESSION_ID,
      player1Token: "tok-p1",
      player2Token: "tok-p2",
      player1Pseudo: "Alice",
      player2Pseudo: "Bob",
      status: "waiting",
      expiresAt: new Date(Date.now() + 60_000),
    },
  });
});

afterAll(async () => {
  await prisma.gameSession.deleteMany({ where: { sessionId: { startsWith: "TEST-AUTH-" } } });
  await prisma.$disconnect();
});

describe("verifyGameSession", () => {
  it("accepts player1 with correct token", async () => {
    const r = await verifyGameSession(TEST_SESSION_ID, "tok-p1");
    expect(r.ok).toBe(true);
    if (r.ok) {
      expect(r.playerNumber).toBe(1);
      expect(r.pseudo).toBe("Alice");
    }
  });

  it("accepts player2 with correct token", async () => {
    const r = await verifyGameSession(TEST_SESSION_ID, "tok-p2");
    expect(r.ok).toBe(true);
    if (r.ok) expect(r.playerNumber).toBe(2);
  });

  it("rejects unknown token", async () => {
    const r = await verifyGameSession(TEST_SESSION_ID, "bogus");
    expect(r.ok).toBe(false);
    if (!r.ok) expect(r.reason).toBe("invalid-token");
  });

  it("rejects unknown sessionId", async () => {
    const r = await verifyGameSession("does-not-exist", "tok-p1");
    expect(r.ok).toBe(false);
    if (!r.ok) expect(r.reason).toBe("session-not-found");
  });

  it("rejects empty inputs", async () => {
    const r = await verifyGameSession("", "");
    expect(r.ok).toBe(false);
    if (!r.ok) expect(r.reason).toBe("missing-credentials");
  });
});
```

- [ ] **Step 2 : Lancer les tests**

```bash
npm test
```
Expected : 5 passed.

- [ ] **Step 3 : Commit**

```bash
git add tests
git commit -m "test(server): unit tests for verifyGameSession"
```

---

### Task 1.6 : Persistence des scores finaux en DB

**Files:**
- Create: `momentum-server/src/db/persistScores.ts`
- Modify: `momentum-server/src/rooms/MomentumRoom.ts` (méthode `determineWinner` + `onDispose`)

- [ ] **Step 1 : Créer `db/persistScores.ts`**

```typescript
import { prisma } from "./prisma";
import { PlayerState } from "../schema/PlayerState";

export async function persistRoomScores(
  gameSessionInternalId: string,
  players: Map<string, PlayerState> | { forEach: (cb: (p: PlayerState) => void) => void }
): Promise<void> {
  const records: Array<{
    playerName: string;
    playerNumber: number;
    totalScore: number;
    distanceTraveled: number;
    survivalTime: number;
    collectiblesCollected: number;
    hasFinished: boolean;
    gameSessionId: string;
  }> = [];

  players.forEach((p) => {
    records.push({
      playerName: p.pseudo,
      playerNumber: p.playerNumber,
      totalScore: p.score,
      distanceTraveled: p.distanceTraveled,
      survivalTime: p.survivalTime,
      collectiblesCollected: p.collectibles,
      hasFinished: p.hasFinished,
      gameSessionId: gameSessionInternalId,
    });
  });

  if (records.length === 0) return;

  await prisma.$transaction([
    prisma.score.createMany({ data: records }),
    prisma.gameSession.update({
      where: { id: gameSessionInternalId },
      data: { status: "finished", finishedAt: new Date() },
    }),
  ]);
}
```

- [ ] **Step 2 : Appeler depuis `MomentumRoom`**

Dans `src/rooms/MomentumRoom.ts`, ajouter à la fin de `determineWinner()` :
```typescript
import { persistRoomScores } from "../db/persistScores";

private async determineWinner() {
  // ... code existant ...

  try {
    await persistRoomScores(this.gameSessionInternalId, this.state.players);
    console.log(`[Room] Scores persisted for ${this.gameSessionInternalId}`);
  } catch (err) {
    console.error(`[Room] Failed to persist scores:`, err);
  }
}
```

⚠️ N'oublie pas de marquer `determineWinner` comme `async`.

- [ ] **Step 3 : Test manuel**

Avec 2 fake clients dans le playground, envoyer `finish` pour les 2 → vérifier dans MySQL que `Score` contient 2 rows et `GameSession.status = 'finished'`.

- [ ] **Step 4 : Commit**

```bash
git add .
git commit -m "feat(server): persist scores and finish gameSession on game end"
```

---

### **Milestone 1 : Critère de validation**

Avant de passer à Milestone 2, valider :
- [ ] `npm run dev` démarre sans erreur
- [ ] Playground `http://localhost:2567/playground` fonctionne
- [ ] Une room rejette les join sans token valide
- [ ] 2 joueurs peuvent join, le 3e est rejeté
- [ ] Le countdown se déclenche à 2 joueurs
- [ ] Le state se synchronise (visible dans monitor)
- [ ] À la fin, scores écrits en DB
- [ ] `npm test` passe

**Si OK** → on peut commencer Milestone 2 ou déployer le serveur en prod (Task M3.4).

---

## Milestone 2 : Refactor Unity pour Mode Web

**Objectif** : un build WebGL "online" du jeu Unity qui se connecte à Colyseus, contrôle le joueur local, et affiche un joueur distant qui bouge en temps réel.

**Critère de succès** : 2 onglets de Chrome ouvrent la même room avec des tokens valides, chaque joueur contrôle son perso, et voit l'autre se déplacer (avec un peu d'interpolation, pas pixel-perfect).

### Task 2.1 : Ajouter scripting define symbols

**Files:**
- Modify: `Assets/Editor/BuildScripts.cs` (CRÉER si n'existe pas — Unity Editor menu)
- Modify: Player Settings (via Unity Editor manuellement)

⚠️ **Action manuelle Unity** (pas scriptable via fichiers proprement) :
1. Open Unity → Edit → Project Settings → Player → Other Settings
2. Scripting Define Symbols : ajouter `ARCADE_BUILD` (par défaut sur la plateforme actuelle)
3. Créer un menu Editor pour basculer rapidement.

- [ ] **Step 1 : Créer `Assets/Editor/BuildScripts.cs`**

```csharp
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;

public static class BuildScripts
{
    [MenuItem("Momentum/Build Mode/Set ARCADE_BUILD")]
    public static void SetArcade()
    {
        SetDefines("ARCADE_BUILD");
    }

    [MenuItem("Momentum/Build Mode/Set WEB_BUILD")]
    public static void SetWeb()
    {
        SetDefines("WEB_BUILD");
    }

    private static void SetDefines(string symbol)
    {
        var target = NamedBuildTarget.FromBuildTargetGroup(
            BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
        PlayerSettings.SetScriptingDefineSymbols(target, symbol);
        AssetDatabase.SaveAssets();
        UnityEngine.Debug.Log($"Defines set to: {symbol}");
    }
}
#endif
```

- [ ] **Step 2 : Tester depuis Unity**

Menu → Momentum → Build Mode → Set ARCADE_BUILD → vérifier dans Player Settings que le define est bien posé.

- [ ] **Step 3 : Commit**

```bash
cd /Users/elouan/Desktop/WS501D/momentum-game-v2
git add Assets/Editor
git commit -m "feat(unity): add build mode toggle (ARCADE_BUILD / WEB_BUILD)"
```

---

### Task 2.2 : Importer le SDK Colyseus dans Unity

**Files:**
- Add: `Assets/Plugins/Colyseus/` (importé via Package Manager)

- [ ] **Step 1 : Importer `colyseus-unity-sdk`**

Dans Unity : Window → Package Manager → "+" → "Add package from git URL..."
URL : `https://github.com/colyseus/colyseus-unity-sdk.git?path=/Assets/Plugins/Colyseus`

⚠️ Si Unity 2022.3 a des soucis avec cette méthode, télécharger le `.unitypackage` depuis https://github.com/colyseus/colyseus-unity-sdk/releases et l'importer manuellement.

- [ ] **Step 2 : Vérifier l'import**

Confirmer la présence de `Colyseus.Client`, `Colyseus.Room` dans les namespaces accessibles. Créer un fichier de test temporaire :
```csharp
using UnityEngine;
using Colyseus;

public class ColyseusSmokeTest : MonoBehaviour
{
    void Start() {
        var client = new ColyseusClient("ws://localhost:2567");
        Debug.Log("Colyseus client created: " + client);
    }
}
```
Attacher à un GameObject vide, lancer Play, vérifier le log. Puis supprimer ce script.

- [ ] **Step 3 : Commit**

```bash
git add Assets/Plugins/Colyseus Packages/manifest.json
git commit -m "feat(unity): import colyseus-unity-sdk"
```

---

### Task 2.3 : Créer `WebBootstrap` (lit URL params)

**Files:**
- Create: `Assets/Scripts/Multiplayer/WebBootstrap.cs`

- [ ] **Step 1 : Créer `WebBootstrap.cs`**

```csharp
using UnityEngine;
using System.Runtime.InteropServices;

public class WebBootstrap : MonoBehaviour
{
    public static string SessionId { get; private set; } = "";
    public static string Token { get; private set; } = "";
    public static bool IsReady { get; private set; } = false;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern string GetUrlParam(string key);
#endif

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        ReadParams();
    }

    private void ReadParams()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        SessionId = GetUrlParam("sessionId") ?? "";
        Token = GetUrlParam("token") ?? "";
#else
        // En Editor, on lit depuis EditorPrefs pour faciliter le dev
        SessionId = PlayerPrefs.GetString("DEBUG_SESSION_ID", "TEST-ROOM");
        Token = PlayerPrefs.GetString("DEBUG_TOKEN", "tok-p1");
#endif
        IsReady = !string.IsNullOrEmpty(SessionId) && !string.IsNullOrEmpty(Token);
        Debug.Log($"[WebBootstrap] sessionId={SessionId}, hasToken={!string.IsNullOrEmpty(Token)}");
    }
}
```

- [ ] **Step 2 : Créer le plugin JS pour lire l'URL**

Créer `Assets/Plugins/WebGL/UrlParams.jslib` :
```javascript
mergeInto(LibraryManager.library, {
  GetUrlParam: function(keyPtr) {
    var key = UTF8ToString(keyPtr);
    var params = new URLSearchParams(window.location.search);
    var value = params.get(key) || "";
    var bufferSize = lengthBytesUTF8(value) + 1;
    var buffer = _malloc(bufferSize);
    stringToUTF8(value, buffer, bufferSize);
    return buffer;
  }
});
```

- [ ] **Step 3 : Commit**

```bash
git add Assets/Scripts/Multiplayer/WebBootstrap.cs Assets/Plugins/WebGL
git commit -m "feat(unity): WebBootstrap reads sessionId/token from URL params"
```

---

### Task 2.4 : Créer `NetworkManager` (singleton connexion + room)

**Files:**
- Create: `Assets/Scripts/Multiplayer/NetworkManager.cs`

⚠️ **Note importante sur le Schema** : le SDK Colyseus Unity a besoin de classes C# qui matchent le schema TypeScript du serveur. Il existe un générateur (`schema-codegen`) — l'exécuter manuellement après chaque modification du schema serveur.

- [ ] **Step 1 : Générer les classes Schema C#**

Depuis le serveur :
```bash
cd /Users/elouan/buts4/www/html/SAE501/momentum-server
npx schema-codegen src/schema/GameState.ts --csharp --output ../../../../Desktop/WS501D/momentum-game-v2/Assets/Scripts/Multiplayer/Schema
```

⚠️ Vérifier que `GameState.cs` et `PlayerState.cs` sont créés dans `Assets/Scripts/Multiplayer/Schema/`.

- [ ] **Step 2 : Créer `NetworkManager.cs`**

```csharp
#if WEB_BUILD
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Colyseus;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    [Header("Server")]
    public string serverUrl = "ws://localhost:2567";

    public ColyseusClient Client { get; private set; }
    public ColyseusRoom<GameState> Room { get; private set; }
    public string MySessionId => Room?.SessionId ?? "";

    public event Action<string, PlayerState> OnPlayerAdded;
    public event Action<string> OnPlayerRemoved;
    public event Action OnConnected;
    public event Action<string> OnConnectionFailed;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    async void Start()
    {
        // Attendre que WebBootstrap ait lu les paramètres
        if (!WebBootstrap.IsReady)
        {
            OnConnectionFailed?.Invoke("Missing sessionId or token in URL");
            return;
        }
        await Connect(WebBootstrap.SessionId, WebBootstrap.Token);
    }

    public async Task Connect(string sessionId, string token)
    {
        try
        {
            Client = new ColyseusClient(serverUrl);
            var options = new Dictionary<string, object>
            {
                { "sessionId", sessionId },
                { "token", token },
            };
            Room = await Client.JoinOrCreate<GameState>("momentum", options);
            Debug.Log($"[NetworkManager] Joined room {Room.RoomId} as {Room.SessionId}");

            Room.State.players.OnAdd += (sessionId, player) => OnPlayerAdded?.Invoke(sessionId, player);
            Room.State.players.OnRemove += (sessionId, _) => OnPlayerRemoved?.Invoke(sessionId);

            OnConnected?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkManager] Connection failed: {ex.Message}");
            OnConnectionFailed?.Invoke(ex.Message);
        }
    }

    public void SendInput(PlayerInputPayload payload) => Room?.Send("input", payload);
    public void SendStun() => Room?.Send("stun");
    public void SendFinish(int score) => Room?.Send("finish", new { score });

    void OnDestroy()
    {
        Room?.Leave();
    }
}

[Serializable]
public class PlayerInputPayload
{
    public float posX, posY, posZ;
    public float velX, velY, velZ;
    public float rotY;
    public bool isGrounded;
    public bool isSliding;
    public float horizontalInput;
}
#endif
```

- [ ] **Step 3 : Commit**

```bash
git add Assets/Scripts/Multiplayer/NetworkManager.cs Assets/Scripts/Multiplayer/Schema
git commit -m "feat(unity): NetworkManager singleton for Colyseus connection"
```

---

### Task 2.5 : `LocalPlayerSync` — envoyer le state local au serveur

**Files:**
- Create: `Assets/Scripts/Multiplayer/LocalPlayerSync.cs`

- [ ] **Step 1 : Créer `LocalPlayerSync.cs`**

```csharp
#if WEB_BUILD
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInput))]
public class LocalPlayerSync : MonoBehaviour
{
    public float sendRateHz = 20f; // doit matcher TICK_RATE_HZ serveur

    private Rigidbody rb;
    private PlayerInput input;
    private PlayerMovement movement;
    private float sendInterval;
    private float timeSinceLastSend;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        input = GetComponent<PlayerInput>();
        movement = GetComponent<PlayerMovement>();
        sendInterval = 1f / sendRateHz;
    }

    void Update()
    {
        timeSinceLastSend += Time.deltaTime;
        if (timeSinceLastSend < sendInterval) return;
        timeSinceLastSend = 0f;

        if (NetworkManager.Instance?.Room == null) return;

        var payload = new PlayerInputPayload
        {
            posX = transform.position.x,
            posY = transform.position.y,
            posZ = transform.position.z,
            velX = rb.velocity.x,
            velY = rb.velocity.y,
            velZ = rb.velocity.z,
            rotY = transform.rotation.eulerAngles.y,
            isGrounded = movement.IsPhysicallyGrounded,
            isSliding = false, // TODO: lire depuis ParkourController si nécessaire
            horizontalInput = input.HorizontalInput,
        };
        NetworkManager.Instance.SendInput(payload);
    }
}
#endif
```

- [ ] **Step 2 : Commit**

```bash
git add Assets/Scripts/Multiplayer/LocalPlayerSync.cs
git commit -m "feat(unity): LocalPlayerSync sends transform/velocity to server at 20Hz"
```

---

### Task 2.6 : `NetworkPlayer` — afficher un joueur distant avec interpolation

**Files:**
- Create: `Assets/Scripts/Multiplayer/NetworkPlayer.cs`

- [ ] **Step 1 : Créer `NetworkPlayer.cs`**

```csharp
#if WEB_BUILD
using UnityEngine;

public class NetworkPlayer : MonoBehaviour
{
    public float interpolationSpeed = 15f;

    private PlayerState state;
    private PlayerAnimator animator;
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    public void Bind(PlayerState playerState)
    {
        state = playerState;
        animator = GetComponentInChildren<PlayerAnimator>();

        targetPosition = new Vector3(state.posX, state.posY, state.posZ);
        targetRotation = Quaternion.Euler(0, state.rotY, 0);
        transform.position = targetPosition;
        transform.rotation = targetRotation;

        // Désactiver tout ce qui est input/physique local sur ce GameObject
        var rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        var input = GetComponent<PlayerInput>(); if (input != null) input.enabled = false;
        var movement = GetComponent<PlayerMovement>(); if (movement != null) movement.enabled = false;
        var parkour = GetComponent<ParkourController>(); if (parkour != null) parkour.enabled = false;

        // Listener pour màj des targets
        state.OnChange += OnStateChange;
    }

    private void OnStateChange(System.Collections.Generic.List<Colyseus.Schema.DataChange> changes)
    {
        targetPosition = new Vector3(state.posX, state.posY, state.posZ);
        targetRotation = Quaternion.Euler(0, state.rotY, 0);
    }

    void Update()
    {
        if (state == null) return;

        transform.position = Vector3.Lerp(transform.position, targetPosition, interpolationSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, interpolationSpeed * Time.deltaTime);

        // Forwarder les flags d'animation
        if (animator != null)
        {
            // TODO: brancher sur ton PlayerAnimator existant
            // animator.SetGrounded(state.isGrounded);
            // animator.SetHorizontalInput(state.horizontalInput);
        }
    }

    void OnDestroy()
    {
        if (state != null) state.OnChange -= OnStateChange;
    }
}
#endif
```

⚠️ Le TODO sur l'animator est volontairement laissé — il faut que tu adaptes selon les méthodes publiques de ton `PlayerAnimator` existant. Au minimum, expose des setters publics pour `isGrounded` et `horizontalInput`.

- [ ] **Step 2 : Commit**

```bash
git add Assets/Scripts/Multiplayer/NetworkPlayer.cs
git commit -m "feat(unity): NetworkPlayer with snapshot interpolation"
```

---

### Task 2.7 : Intégration dans `GameSessionManager` — spawn local vs remote

**Files:**
- Modify: `Assets/Scripts/GameSessionManager.cs`
- Modify: `Assets/Scripts/SplitScreenManager.cs`
- Modify: `Assets/Scripts/PlayerScripts/PlayerInput.cs`

⚠️ Avant cette tâche, **lis** `GameSessionManager.cs` en entier — le code ci-dessous suppose qu'il a une méthode pour spawn les joueurs ; adapte aux noms réels.

- [ ] **Step 1 : Wrap `SplitScreenManager` pour le désactiver en WEB_BUILD**

En haut de `Assets/Scripts/SplitScreenManager.cs`, wrapper toute la classe :
```csharp
#if !WEB_BUILD
using UnityEngine;

public class SplitScreenManager : MonoBehaviour
{
    // ... contenu existant inchangé ...
}
#endif
```

- [ ] **Step 2 : Forcer playerID=1 en WEB_BUILD dans `PlayerInput`**

Dans `Start()` de `PlayerInput.cs`, juste après le code existant :
```csharp
void Start()
{
#if WEB_BUILD
    playerID = 1; // En multi online, le joueur local est toujours P1 sur son client
#endif
    horizontalAxisName = "P" + playerID + "_Horizontal";
    // ... reste inchangé
}
```

- [ ] **Step 3 : Modifier `GameSessionManager` pour spawn web**

Identifier la méthode qui spawn les 2 joueurs en mode arcade. Ajouter un branchement :
```csharp
#if WEB_BUILD
    SpawnForWeb();
#else
    SpawnForArcade(); // ton code existant
#endif

#if WEB_BUILD
private void SpawnForWeb()
{
    // Spawn le joueur local (un seul) en P1
    var localPlayer = Instantiate(playerPrefab, spawnP1.position, spawnP1.rotation);
    localPlayer.AddComponent<LocalPlayerSync>();

    // Listen sur NetworkManager pour spawn les NetworkPlayers distants
    NetworkManager.Instance.OnPlayerAdded += HandleRemotePlayerAdded;
    NetworkManager.Instance.OnPlayerRemoved += HandleRemotePlayerRemoved;
}

private Dictionary<string, GameObject> remotePlayers = new();

private void HandleRemotePlayerAdded(string sessionId, PlayerState state)
{
    if (sessionId == NetworkManager.Instance.MySessionId) return; // c'est moi

    Transform spawn = (state.playerNumber == 1) ? spawnP1 : spawnP2;
    var go = Instantiate(playerPrefab, spawn.position, spawn.rotation);
    var netPlayer = go.AddComponent<NetworkPlayer>();
    netPlayer.Bind(state);
    remotePlayers[sessionId] = go;
}

private void HandleRemotePlayerRemoved(string sessionId)
{
    if (remotePlayers.TryGetValue(sessionId, out var go))
    {
        Destroy(go);
        remotePlayers.Remove(sessionId);
    }
}
#endif
```

⚠️ Adapte les noms : `playerPrefab`, `spawnP1`, `spawnP2` sont des hypothèses. Vérifier les vrais champs dans `GameSessionManager.cs` avant de coller.

- [ ] **Step 4 : Test manuel en Editor**

1. Set ARCADE_BUILD via menu → Play → vérifier que le mode arcade local marche encore (régression test).
2. Set WEB_BUILD → s'assurer que le serveur Colyseus tourne (`npm run dev` côté serveur) → Play → l'Editor lit `DEBUG_SESSION_ID="TEST-ROOM"` et `DEBUG_TOKEN="tok-p1"` (insérés en DB via Task 1.4).
3. Vérifier la console : `[NetworkManager] Joined room`. Le joueur local bouge, pas de joueur distant pour l'instant.

- [ ] **Step 5 : Commit**

```bash
git add Assets/Scripts
git commit -m "feat(unity): branch GameSessionManager for WEB_BUILD multiplayer"
```

---

### Task 2.8 : RPC réseau pour le stun (`InterferenceSystem`)

**Files:**
- Modify: `Assets/Scripts/Multiplayer/InterferenceSystem.cs`

- [ ] **Step 1 : Ajouter le branchement réseau**

Modifier `AttemptInterference` :
```csharp
public void AttemptInterference(int attackerPlayerID)
{
#if WEB_BUILD
    // En mode online, on demande au serveur d'arbitrer
    if (NetworkManager.Instance?.Room != null)
    {
        NetworkManager.Instance.SendStun();
    }
#else
    // Logique existante (split-screen local)
    if (attackerPlayerID == 1 && player2 != null) player2.ApplyStun(stunDuration);
    else if (attackerPlayerID == 2 && player1 != null) player1.ApplyStun(stunDuration);
#endif
}
```

- [ ] **Step 2 : Ajouter un listener qui applique le stun localement**

Créer une nouvelle méthode dans `InterferenceSystem` (toujours dans `#if WEB_BUILD`) :
```csharp
#if WEB_BUILD
void Start()
{
    if (NetworkManager.Instance != null)
    {
        NetworkManager.Instance.OnPlayerAdded += (sessionId, state) =>
        {
            state.OnChange += (changes) => {
                if (sessionId == NetworkManager.Instance.MySessionId)
                {
                    foreach (var c in changes)
                    {
                        if (c.Field == "isStunned" && (bool)c.Value)
                        {
                            // Le serveur dit que je suis stun
                            FindLocalPlayerMovement()?.ApplyStun(stunDuration);
                        }
                    }
                }
            };
        };
    }
}

private PlayerMovement FindLocalPlayerMovement()
{
    var local = FindObjectOfType<LocalPlayerSync>();
    return local?.GetComponent<PlayerMovement>();
}
#endif
```

- [ ] **Step 3 : Test manuel à 2 onglets**

(Voir Task 2.9 pour le setup multi-onglet en Editor — pour l'instant marker comme à valider.)

- [ ] **Step 4 : Commit**

```bash
git add Assets/Scripts/Multiplayer/InterferenceSystem.cs
git commit -m "feat(unity): route stun through Colyseus server in WEB_BUILD"
```

---

### Task 2.9 : Test à 2 instances et fix interpolation

**Files:** aucun nouveau, validation manuelle

- [ ] **Step 1 : Préparer 2 GameSessions de test en DB**

```sql
INSERT INTO game_session (id, sessionId, player1Token, player2Token, status, expiresAt, createdAt)
VALUES ('test-multi', 'TEST-ROOM', 'tok-p1', 'tok-p2', 'waiting',
        DATE_ADD(NOW(), INTERVAL 1 HOUR), NOW());
```

- [ ] **Step 2 : Build WebGL "WEB_BUILD"**

Menu → Momentum → Build Mode → Set WEB_BUILD
File → Build Settings → WebGL → Build → choisir un dossier (ex: `Build/Web-Multi-Test`)

- [ ] **Step 3 : Servir le build localement**

```bash
cd /Users/elouan/Desktop/WS501D/momentum-game-v2/Build/Web-Multi-Test
python3 -m http.server 8080
```

- [ ] **Step 4 : Ouvrir 2 onglets**

Onglet 1 : `http://localhost:8080/?sessionId=TEST-ROOM&token=tok-p1`
Onglet 2 : `http://localhost:8080/?sessionId=TEST-ROOM&token=tok-p2`

⚠️ Vérifier que le serveur Colyseus tourne (`npm run dev` dans `momentum-server/`).

- [ ] **Step 5 : Valider le critère de succès Milestone 2**

Dans chaque onglet :
- [ ] Console : `[NetworkManager] Joined room`
- [ ] Le joueur local bouge avec les inputs clavier
- [ ] Le joueur distant apparaît à son spawn
- [ ] Quand l'autre joueur bouge dans son onglet, je le vois bouger dans le mien (avec un peu de délai/lissage)
- [ ] Le countdown se déclenche dans les 2 onglets en même temps

⚠️ Si l'interpolation est trop saccadée, ajuster `interpolationSpeed` dans `NetworkPlayer.cs` (entre 10 et 25). Si trop "élastique", augmenter `sendRateHz` à 30.

- [ ] **Step 6 : Commit éventuel des tweaks**

```bash
git add -A
git commit -m "tweak(unity): adjust network interpolation params after testing"
```

---

### **Milestone 2 : Critère de validation**

Avant de passer à Milestone 3, valider :
- [ ] Mode arcade local marche toujours (régression OK)
- [ ] Build WEB_BUILD se compile sans erreur
- [ ] 2 onglets se synchronisent visuellement
- [ ] Stun fonctionne entre les 2 joueurs (validé via test manuel à 2 onglets)
- [ ] Le score s'envoie au serveur quand le joueur franchit la ligne d'arrivée

---

## Milestone 3 : Lobby Web Next.js + Déploiement VPS

**Objectif** : un joueur va sur le site, crée une room, partage un lien, son pote rejoint, ils jouent. Tout déployé en prod sur le VPS.

**Critère de succès** : depuis 2 ordinateurs distants, on peut faire une partie complète (création room → 2e joueur join via lien → jeu → score en DB).

### Task 3.1 : Endpoint `POST /api/lobby/create`

**Files:**
- Create: `momentum/src/app/api/lobby/create/route.ts`
- Create: `momentum/src/lib/colyseus-admin.ts`

⚠️ Cette route doit être **authentifiée** via Better Auth session (pas anonyme). Seul un user connecté peut créer une room.

- [ ] **Step 1 : Créer `lib/colyseus-admin.ts`**

```typescript
// Wrapper pour appeler l'API HTTP du game server (matchmaker)
const COLYSEUS_HTTP_URL = process.env.COLYSEUS_HTTP_URL || "http://localhost:2567";

export async function createColyseusRoom(gameSessionId: string): Promise<{ roomId: string }> {
  // En Colyseus 0.15, on peut créer une room via HTTP matchmake API.
  // Voir https://docs.colyseus.io/server/matchmaker/
  const res = await fetch(`${COLYSEUS_HTTP_URL}/matchmake/create/momentum`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ gameSessionId }),
  });
  if (!res.ok) throw new Error(`Colyseus create failed: ${res.status}`);
  const data = await res.json();
  return { roomId: data.room.roomId };
}
```

- [ ] **Step 2 : Créer `app/api/lobby/create/route.ts`**

```typescript
import { NextRequest, NextResponse } from "next/server";
import { auth } from "@/lib/auth";
import { prisma } from "@/lib/prisma";
import { randomBytes } from "crypto";

function generateSessionId(): string {
  return randomBytes(4).toString("hex").toUpperCase();
}
function generateToken(): string {
  return randomBytes(32).toString("hex");
}

export async function POST(req: NextRequest) {
  const session = await auth.api.getSession({ headers: req.headers });
  if (!session) return NextResponse.json({ error: "unauthorized" }, { status: 401 });

  const body = await req.json().catch(() => ({}));
  const pseudo = body.pseudo ?? session.user.name;

  const gameSession = await prisma.gameSession.create({
    data: {
      sessionId: generateSessionId(),
      player1Token: generateToken(),
      player2Token: generateToken(),
      player1Pseudo: pseudo,
      status: "waiting",
      expiresAt: new Date(Date.now() + 60 * 60 * 1000),
    },
  });

  return NextResponse.json({
    sessionId: gameSession.sessionId,
    player1Token: gameSession.player1Token,
    inviteUrl: `/play/${gameSession.sessionId}?p=2`,
    playUrl: `/play/${gameSession.sessionId}?p=1`,
  });
}
```

- [ ] **Step 3 : Endpoint pour rejoindre (POST `/api/lobby/[sessionId]/join`)**

Créer `app/api/lobby/[sessionId]/join/route.ts` :
```typescript
import { NextRequest, NextResponse } from "next/server";
import { auth } from "@/lib/auth";
import { prisma } from "@/lib/prisma";

export async function POST(req: NextRequest, ctx: { params: Promise<{ sessionId: string }> }) {
  const { sessionId } = await ctx.params;
  const session = await auth.api.getSession({ headers: req.headers });
  if (!session) return NextResponse.json({ error: "unauthorized" }, { status: 401 });

  const body = await req.json().catch(() => ({}));
  const pseudo = body.pseudo ?? session.user.name;

  const gs = await prisma.gameSession.findUnique({ where: { sessionId } });
  if (!gs) return NextResponse.json({ error: "session-not-found" }, { status: 404 });
  if (gs.status !== "waiting") return NextResponse.json({ error: "session-not-joinable" }, { status: 400 });
  if (gs.player2Joined) return NextResponse.json({ error: "session-full" }, { status: 400 });

  await prisma.gameSession.update({
    where: { id: gs.id },
    data: { player2Pseudo: pseudo },
  });

  return NextResponse.json({
    sessionId: gs.sessionId,
    player2Token: gs.player2Token,
  });
}
```

- [ ] **Step 4 : Tester avec curl**

```bash
# Créer
curl -X POST http://localhost:3000/api/lobby/create \
  -H "Cookie: better-auth.session_token=..." \
  -d '{"pseudo":"Alice"}'
# → { sessionId: "ABCD1234", player1Token: "...", inviteUrl, playUrl }

# Joindre
curl -X POST http://localhost:3000/api/lobby/ABCD1234/join \
  -H "Cookie: better-auth.session_token=..." \
  -d '{"pseudo":"Bob"}'
# → { sessionId, player2Token }
```

- [ ] **Step 5 : Commit**

```bash
cd /Users/elouan/buts4/www/html/SAE501/momentum
git add src/app/api/lobby src/lib/colyseus-admin.ts
git commit -m "feat(api): lobby create + join endpoints"
```

---

### Task 3.2 : Pages UI lobby

**Files:**
- Create: `momentum/src/app/lobby/page.tsx`
- Create: `momentum/src/app/lobby/[sessionId]/page.tsx`
- Create: `momentum/src/app/play/[sessionId]/page.tsx`

⚠️ Ces pages utilisent les composants `@heroui/react` et `@radix-ui/*` déjà installés. Adapter au design system existant en regardant `src/components/`.

- [ ] **Step 1 : `app/lobby/page.tsx` — page de création**

```tsx
"use client";
import { useState } from "react";
import { useRouter } from "next/navigation";

export default function LobbyPage() {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  async function createGame() {
    setLoading(true);
    setError("");
    try {
      const res = await fetch("/api/lobby/create", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({}),
      });
      if (!res.ok) throw new Error(await res.text());
      const data = await res.json();
      // Stocker le token dans sessionStorage pour la page /play
      sessionStorage.setItem(`token-${data.sessionId}`, data.player1Token);
      router.push(`/lobby/${data.sessionId}`);
    } catch (e: any) {
      setError(e.message);
      setLoading(false);
    }
  }

  return (
    <div className="container mx-auto py-12">
      <h1 className="text-3xl font-bold mb-6">Multijoueur en ligne</h1>
      <button
        onClick={createGame}
        disabled={loading}
        className="px-6 py-3 bg-blue-600 text-white rounded-lg disabled:opacity-50"
      >
        {loading ? "Création..." : "Créer une partie"}
      </button>
      {error && <p className="mt-4 text-red-500">{error}</p>}
    </div>
  );
}
```

- [ ] **Step 2 : `app/lobby/[sessionId]/page.tsx` — waiting room**

```tsx
"use client";
import { useEffect, useState, use } from "react";
import { useRouter } from "next/navigation";

export default function WaitingRoom({ params }: { params: Promise<{ sessionId: string }> }) {
  const { sessionId } = use(params);
  const router = useRouter();
  const [status, setStatus] = useState<"waiting" | "ready">("waiting");
  const [copied, setCopied] = useState(false);
  const inviteUrl = typeof window !== "undefined"
    ? `${window.location.origin}/play/${sessionId}?role=join`
    : "";

  // Poll toutes les 2s pour voir si le 2e joueur a rejoint
  useEffect(() => {
    const i = setInterval(async () => {
      const r = await fetch(`/api/lobby/${sessionId}/status`);
      if (!r.ok) return;
      const data = await r.json();
      if (data.player2Joined || data.status === "playing") {
        setStatus("ready");
        clearInterval(i);
        // Auto-redirect après 1s
        setTimeout(() => router.push(`/play/${sessionId}?role=host`), 1000);
      }
    }, 2000);
    return () => clearInterval(i);
  }, [sessionId, router]);

  return (
    <div className="container mx-auto py-12 text-center">
      <h1 className="text-3xl font-bold mb-6">Partie {sessionId}</h1>
      <p className="mb-4">Partage ce lien avec ton ami :</p>
      <div className="flex items-center justify-center gap-2 mb-6">
        <code className="px-4 py-2 bg-gray-100 rounded">{inviteUrl}</code>
        <button onClick={() => { navigator.clipboard.writeText(inviteUrl); setCopied(true); }}>
          {copied ? "✓" : "Copier"}
        </button>
      </div>
      <p>{status === "waiting" ? "En attente du 2e joueur..." : "Lancement de la partie !"}</p>
    </div>
  );
}
```

⚠️ Cette page utilise un endpoint `/api/lobby/[sessionId]/status` non encore créé. Le créer maintenant :

```typescript
// app/api/lobby/[sessionId]/status/route.ts
import { NextRequest, NextResponse } from "next/server";
import { prisma } from "@/lib/prisma";

export async function GET(_: NextRequest, ctx: { params: Promise<{ sessionId: string }> }) {
  const { sessionId } = await ctx.params;
  const gs = await prisma.gameSession.findUnique({
    where: { sessionId },
    select: { status: true, player1Joined: true, player2Joined: true },
  });
  if (!gs) return NextResponse.json({ error: "not-found" }, { status: 404 });
  return NextResponse.json(gs);
}
```

- [ ] **Step 3 : `app/play/[sessionId]/page.tsx` — embed du WebGL**

```tsx
"use client";
import { useEffect, useState, use } from "react";
import { useSearchParams } from "next/navigation";

export default function PlayPage({ params }: { params: Promise<{ sessionId: string }> }) {
  const { sessionId } = use(params);
  const searchParams = useSearchParams();
  const role = searchParams.get("role");
  const [token, setToken] = useState<string | null>(null);

  useEffect(() => {
    if (role === "host") {
      setToken(sessionStorage.getItem(`token-${sessionId}`));
    } else {
      // Joiner : appeler l'endpoint pour récupérer le token P2
      fetch(`/api/lobby/${sessionId}/join`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({}),
      })
        .then((r) => r.json())
        .then((d) => setToken(d.player2Token));
    }
  }, [sessionId, role]);

  if (!token) return <div className="p-8">Connexion à la partie...</div>;

  const iframeSrc = `/webgl/index.html?sessionId=${sessionId}&token=${token}`;
  return (
    <iframe src={iframeSrc} className="w-screen h-screen border-0" allow="autoplay; gamepad" />
  );
}
```

- [ ] **Step 4 : Commit**

```bash
git add src/app/lobby src/app/play src/app/api/lobby
git commit -m "feat(web): lobby UI + waiting room + game embed"
```

---

### Task 3.3 : Servir le build WebGL depuis Next.js

**Files:**
- Modify: `momentum/public/webgl/` (copier le build Unity)
- Modify: `momentum/next.config.ts` (headers pour WebGL)

- [ ] **Step 1 : Copier le build dans `public/webgl`**

```bash
# Build WEB_BUILD côté Unity (déjà fait Task 2.9)
cp -r /Users/elouan/Desktop/WS501D/momentum-game-v2/Build/Web-Multi-Test/* \
      /Users/elouan/buts4/www/html/SAE501/momentum/public/webgl/
```

- [ ] **Step 2 : Configurer headers Next.js (gzip/brotli WebGL)**

Modifier `momentum/next.config.ts` :
```typescript
const nextConfig = {
  // ... existant
  async headers() {
    return [
      {
        source: "/webgl/:path*.gz",
        headers: [{ key: "Content-Encoding", value: "gzip" }],
      },
      {
        source: "/webgl/:path*.br",
        headers: [{ key: "Content-Encoding", value: "br" }],
      },
      {
        source: "/webgl/:path*",
        headers: [{ key: "Cross-Origin-Embedder-Policy", value: "require-corp" }],
      },
    ];
  },
};
export default nextConfig;
```

⚠️ Selon le compression mode choisi dans Unity Build Settings (Disabled/Gzip/Brotli), adapter ces headers.

- [ ] **Step 3 : Test end-to-end en local**

1. `npm run dev` côté Next.js (port 3000)
2. `npm run dev` côté Colyseus (port 2567)
3. Naviguer vers `http://localhost:3000/lobby`
4. Connecté avec un compte → "Créer une partie" → URL d'invitation
5. Ouvrir l'URL d'invitation dans un autre navigateur (ou onglet privé) connecté avec un autre compte
6. Les 2 doivent être redirigés vers `/play/<sessionId>` et la partie démarre

- [ ] **Step 4 : Commit**

```bash
git add public/webgl next.config.ts
git commit -m "feat(web): serve Unity WebGL build under /webgl with proper headers"
```

⚠️ Note : `public/webgl/` est volumineux (~50-100Mo). Considère exclure du repo via `.gitignore` et le déployer séparément (script de copie au déploiement).

---

### Task 3.4 : Déploiement VPS

**Files:** configuration serveur uniquement (pas de fichiers du projet)

⚠️ Cette tâche suppose un accès SSH au VPS `51.178.85.130`. Adapter selon la config réelle.

- [ ] **Step 1 : Installer Node 20 + PM2 sur le VPS**

```bash
ssh user@51.178.85.130
curl -fsSL https://deb.nodesource.com/setup_20.x | sudo -E bash -
sudo apt install -y nodejs
sudo npm install -g pm2
```

- [ ] **Step 2 : Déployer momentum-server**

```bash
# Sur le VPS
git clone <repo-url-de-momentum-server> /var/www/momentum-server
cd /var/www/momentum-server
npm ci
npm run build
cp .env.example .env  # éditer avec les vraies valeurs
pm2 start dist/index.js --name momentum-server
pm2 startup
pm2 save
```

- [ ] **Step 3 : Configurer reverse proxy nginx**

Créer `/etc/nginx/sites-available/momentum-game` :
```nginx
server {
  listen 443 ssl http2;
  server_name game.tondomaine.fr;

  ssl_certificate /etc/letsencrypt/live/game.tondomaine.fr/fullchain.pem;
  ssl_certificate_key /etc/letsencrypt/live/game.tondomaine.fr/privkey.pem;

  location / {
    proxy_pass http://localhost:2567;
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;       # WebSocket
    proxy_set_header Connection "upgrade";
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_read_timeout 3600s;
  }
}
```
Activer et reload :
```bash
sudo ln -s /etc/nginx/sites-available/momentum-game /etc/nginx/sites-enabled/
sudo nginx -t && sudo systemctl reload nginx
sudo certbot --nginx -d game.tondomaine.fr
```

- [ ] **Step 4 : Mettre à jour les URLs dans le code**

Côté Unity (`NetworkManager.cs`) :
```csharp
public string serverUrl = "wss://game.tondomaine.fr";
```

Côté Next.js, `momentum/.env.production` :
```env
COLYSEUS_HTTP_URL=https://game.tondomaine.fr
```

Côté `momentum-server/.env` (sur le VPS) :
```env
NEXT_API_URL=https://tondomaine.fr  # le site Next.js
```

- [ ] **Step 5 : Re-build Unity + re-déployer site**

Commit, push, redeploy via le pipeline existant du site Next.js.

- [ ] **Step 6 : Test end-to-end production**

Depuis 2 ordinateurs distants : créer une room, partager le lien, jouer une partie complète. Vérifier dans MySQL que `Score` contient les 2 rows et `GameSession.status = 'finished'`.

- [ ] **Step 7 : Commit**

```bash
git commit -m "feat(deploy): production URLs and nginx config for game server"
```

---

### **Milestone 3 : Critère de validation**

- [ ] 2 utilisateurs sur 2 machines distantes peuvent jouer une partie complète
- [ ] Les scores sont persistés en DB
- [ ] Le mode arcade local marche toujours en parallèle
- [ ] Pas d'erreur dans `pm2 logs momentum-server` pendant 30 min de jeu

---

## Checklist anti-régression finale

- [ ] Mode arcade : un build ARCADE_BUILD démarre et joue normalement (split-screen, P1+P2 sur le même clavier)
- [ ] Mode web : un build WEB_BUILD se connecte au serveur, contrôle 1 joueur, voit le 2e bouger
- [ ] Auth : un token invalide est rejeté côté Colyseus
- [ ] DB : la table `game_session` se met à jour correctement (`player2Joined`, `status`, `finishedAt`)
- [ ] DB : la table `score` reçoit 2 lignes par partie terminée
- [ ] Régression : le système Anatidae de highscores arcade fonctionne toujours

---

## Risques connus & mitigations

| Risque | Mitigation |
|---|---|
| Latence réseau visible (joueur distant "saccade") | Augmenter `sendRateHz` à 30, tweaker `interpolationSpeed`. Si insuffisant, passer à un buffer d'interpolation 100ms (snapshot interpolation classique). |
| Joueurs sur des réseaux mobiles (jitter élevé) | Ajouter un système d'extrapolation basé sur la velocity. Hors scope POC. |
| Joueur quitte en cours → l'autre reste seul | Géré : `onLeave` met `status = finished` |
| Schema Colyseus drift entre serveur TS et client C# | Toujours regénérer via `schema-codegen` après modification du schema serveur. Ajouter un hook git ou script CI. |
| WebGL "stuck loading" | Vérifier la compression (Disabled au début, activer Brotli plus tard). Vérifier les headers Cross-Origin. |
| MySQL connexions épuisées (Colyseus + Next.js) | Configurer `connection_limit` dans `DATABASE_URL`, ex: `?connection_limit=10`. Surveiller via Prisma metrics. |
| Anti-cheat absent (un client peut envoyer n'importe quelle position) | Acceptable pour le POC SAE501. Pour la prod publique, ajouter validation de vitesse max + collision serveur. |

---

## Ordre d'exécution recommandé

1. **Milestone 1** (game server seul) — testable à 100% via playground, aucune dépendance Unity ou Next.js
2. **Milestone 2** (Unity refactor) — débloque le test à 2 onglets
3. **Milestone 3** (lobby + déploiement) — finalise l'expérience utilisateur

Chaque milestone est livrable et testable indépendamment. Si tu manques de temps, tu peux **stopper après Milestone 2** et garder un système où tu construis manuellement les URLs avec tokens (utilisable mais pas user-friendly).
