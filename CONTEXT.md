# OpenMU — Custom Server Context

## Key Docs
- `docs/verification.md` — how to verify each custom update applied correctly (admin panel + DB queries)
- `docs/specs/pvp-balance.md` — PvP class multiplier spec
- `docs/specs/server-configuration.md` — server rates and jewel config spec

## What this project is
A fork of [MUnique/OpenMU](https://github.com/MUnique/OpenMU) — an open-source C#/.NET reimplementation of the MU Online MMORPG server targeting **Season 6 Episode 3**.

Fork: `github.com/delellisgabriel/OpenMU`
Upstream: `github.com/MUnique/OpenMU`

Custom changes live in feature branches. `master` stays in sync with upstream.
Current active branch: `custom/pvp-balance`

---

## Infrastructure

### Docker (all-in-one)
- Config: `deploy/all-in-one/docker-compose.yml`
- Custom image name: `delellisgabriel/openmu`
- Three containers: `nginx-80` (admin panel), `openmu-startup` (game server), `database` (PostgreSQL)
- User data persists in named Docker volume `dbdata` — never run `docker compose down -v`

**Rebuild workflow (after code changes):**
```powershell
# From D:\OpenMU
docker build -f src/Startup/Dockerfile -t delellisgabriel/openmu .
# From D:\OpenMU\deploy\all-in-one
docker compose up -d --no-deps openmu-startup
```

### Admin Panel
- URL: `http://localhost`
- Credentials: `admin` / `openmu`
- Used for: starting/stopping servers, configuring drops, plugins, character classes, IP resolver

### Game Client
- Path: `D:\client\MU Client 1.04d - Season 6E3\MU Client 1.04d - Season 6E3\`
- Launcher: `MUnique.OpenMU.ClientLauncher.exe` (in client folder)
- Server address: `127.127.127.127:44405` (127.0.0.1 is blocked by the client)
- IP Resolver set to `Custom` → `127.127.127.127` in admin panel (Configuration → System)

---

## Codebase Map

### Damage calculation
`src/GameLogic/AttackableExtensions.cs` — `CalculateDamageAsync()`
Single function handling all damage. Key pipeline:
1. Hit check (attack rate vs defense rate)
2. Base damage from attacker stats + skill
3. Defense subtracted
4. Multipliers applied (`AttackDamageIncrease`, `DamageReceiveDecrement`)
5. PvP-specific bonuses (`FinalDamageIncreasePvp`)

### Attribute system
`src/GameLogic/Attributes/Stats.cs` — all `AttributeDefinition` constants
`src/DataModel/Configuration/` — configuration models (CharacterClass, GameConfiguration, etc.)

### Character class initialization
`src/Persistence/Initialization/CharacterClasses/` — one file per class
Each class defines base stats, stat scaling formulas, and attribute relationships.

### Season 6 game data
`src/Persistence/Initialization/VersionSeasonSix/` — maps, monsters, items, skills, quests

### Drop system
`src/GameLogic/DefaultDropGenerator.cs` — item drop logic
`src/Persistence/Initialization/Items/` — item definitions including boot WalkSpeed

---

## Character Classes

All classes share: HP = `base + (VIT × multiplier) + (Level × multiplier)`

| Class | Evolution | STR | AGI | VIT | ENE | Pts/Lvl | Defense/AGI | HP/VIT |
|---|---|---|---|---|---|---|---|---|
| Dark Knight | → Blade Knight → Blade Master | 28 | 20 | 25 | 10 | 5 | ÷3 (best) | ×3 (best) |
| Dark Wizard | → Soul Master → Grand Master | 18 | 18 | 15 | 30 | 5 | ×0.25 | ×2 |
| Fairy Elf | → Muse Elf → High Elf | 22 | 25 | 20 | 15 | 5 | ÷10 (worst) | ×2 |
| Magic Gladiator | → Duel Master | 26 | 26 | 26 | 26 | 7 | ÷5 | ×2 |
| Dark Lord | → Lord Emperor | 26 | 20 | 20 | 15 | 7 | ÷7 | ×2 |
| Rage Fighter | → Fist Master | 32 | 27 | 25 | 20 | 7 | ÷8 | ×2 |

### Damage sources per class
- **Dark Knight**: Physical — `STR÷6` (min) to `STR÷4` (max)
- **Dark Wizard**: Wizardry — `ENE÷9` (min) to `ENE÷4` (max)
- **Fairy Elf**: Archery — `AGI÷7` (min) to `AGI÷4` (max) + minor STR contribution
- **Magic Gladiator**: Physical (STR) + Wizardry (ENE) — hybrid
- **Dark Lord**: Physical (STR) + Raven pet (scales with CMD/Leadership)
- **Rage Fighter**: Physical — `STR÷7~÷5` + unique `VIT÷15~÷12` (only class with VIT→damage)

### Attack speed per AGI
- Rage Fighter: AGI÷9 (fastest)
- Dark Knight / Magic Gladiator: AGI÷15
- Dark Lord: AGI÷10
- Dark Wizard: AGI÷20 physical / AGI÷10 magic
- Fairy Elf: AGI÷50 (slowest — both physical and magic)

---

## Known PvP Imbalances

1. **Dark Wizard burst** — `ENE÷4` max damage with full ENE build hits extremely hard. No class-specific resistance exists.
2. **Fairy Elf defense** — `AGI÷10` is 3× worse than Dark Knight (`AGI÷3`). Elf is intentionally weak in 1v1 PvP (support/buffer role) but the gap is severe.
3. **No class vs class multiplier system** — OpenMU has no mechanism to apply different damage multipliers based on attacker/defender class combination. This is the primary planned feature.

---

## Planned Customizations

### class vs class PvP multipliers (`custom/pvp-balance`)
Add per-matchup damage reduction attributes to the character class system.
Spec: `docs/specs/pvp-balance.md`

Files to change:
- `src/GameLogic/Attributes/Stats.cs` — new AttributeDefinitions
- `src/GameLogic/AttackableExtensions.cs` — apply multipliers in `CalculateDamageAsync`
- `src/Persistence/Initialization/CharacterClasses/*.cs` — set default values per class

---

## Drop System Key Facts
- `ExcellentItemDropLevelDelta = 25` — excellent item pool = `monsterLevel - 25`
- `DropLevelMaxGap = 12` (hardcoded) — only items within 12 levels below monster drop
- Item `+level` on drop = `(monsterLevel - item.DropLevel) / 3`
- `MaximumItemOptionLevelDrop` (default 3, range 1–4) — configurable in admin panel

## Jewel System Key Facts
- Soul Jewel: 50% base success, +25% with luck, fails on +7+ reset item to +0
- Configurable in admin panel: Configuration → Plugin Configurations

## Movement Speed
- `WalkSpeed` comes only from boots (per item definition), not from character class
- Enforced client-side only — server sends the value, client uses it
- Wings provide speed boost client-side regardless of server stats

---

## Test Accounts
All passwords = username.

| Account | Notes |
|---|---|
| test0–test9 | Levels 1–90 in 10-step increments |
| test300 / test400 | High level general accounts |
| testgm / testgm2 | Game master accounts |
| ancient / socket | Accounts with special item sets |
