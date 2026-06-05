# OpenMU — Custom Server Context

## Working Agreement
- **Never commit and push in the same step.** Always commit first, show the diff summary, and wait for explicit approval before pushing.
- **Never push with `--force` to `master`.** Feature branches are fine.
- Spec must exist and be reviewed before implementation starts.

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
5. **Custom:** PvP class vs class multiplier (`DamageReceiveFromX`) applied after step 4
6. PvP-specific bonuses (`FinalDamageIncreasePvp`)

`GetClassDamageReceiveStat(Player attacker)` — private helper that maps attacker's class number (raw byte) to the correct `DamageReceiveFromX` attribute. Uses raw bytes to avoid a dependency from `GameLogic` on `Persistence.Initialization`.

### Attribute system
`src/GameLogic/Attributes/Stats.cs` — all `AttributeDefinition` constants
Each `AttributeDefinition` is a named slot (like a variable declaration) with a permanent `Guid` as its database key. The actual per-class values are stored separately as `ConstValueAttribute` entries.
**Custom attributes added:** `DamageReceiveFromDarkKnightDecrement`, `DamageReceiveFromDarkWizardDecrement`, `DamageReceiveFromFairyElfDecrement`, `DamageReceiveFromMagicGladiatorDecrement`, `DamageReceiveFromDarkLordDecrement`, `DamageReceiveFromRageFighterDecrement`

`src/DataModel/Configuration/` — configuration models (CharacterClass, GameConfiguration, etc.)

### Character class initialization
`src/Persistence/Initialization/CharacterClasses/` — one file per class
Each class defines base stats, stat scaling formulas, and attribute relationships.
`CharacterClassInitialization.cs` → `AddCommonBaseAttributeValues()` — shared block called by every class. **Custom:** seeds all 6 `DamageReceiveFromX` attributes at `1.0f` (neutral) for fresh DB installs.

### Update plugins
`src/Persistence/Initialization/Updates/` — one plugin per schema change
Each plugin has a version number (`UpdateVersion` enum), runs once on existing databases when triggered from admin panel → Updates, and is idempotent (safe to run twice).
**Custom plugins:**
- `CustomServerRatesUpdatePlugIn` (v100) — sets experience ×1000, drop delta 10, option level 4
- `CustomJewelRatesUpdatePlugIn` (v101) — sets Soul Jewel 70%/30%/+8 reset
- `AddPvpClassMultipliersUpdatePlugIn` (v84) — registers the 6 new `AttributeDefinition`s and seeds `1.0f` on every existing character class

**Version numbering:** upstream uses 1–99, custom starts at 100. Exception: v84 sits between upstream entries because it directly extends the character class system.

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

1. **Dark Wizard burst** — `ENE÷4` max damage with full ENE build hits extremely hard.
2. **Fairy Elf defense** — `AGI÷10` is 3× worse than Dark Knight (`AGI÷3`). Intentionally weak in 1v1 but the gap is severe.

---

## Implemented Customizations

### Class vs class PvP multipliers (`custom/pvp-balance`) — ✅ implemented
Per-matchup damage reduction via `DamageReceiveFromX` attributes on each character class.
All values default to `1.0` (neutral). Tune per-matchup in admin panel → Configuration → Character Classes.
Spec: `docs/specs/pvp-balance.md`

**How to tune:** find the defending class in the admin panel, locate the `DamageReceiveFromX` attribute for the attacker class, set a value below 1.0 to reduce incoming damage (e.g. `0.75` = 25% reduction).

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
