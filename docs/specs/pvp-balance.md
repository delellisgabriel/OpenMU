# Spec: Class vs Class PvP Damage Multipliers

## Status
`implemented` — branch `custom/pvp-balance`

## Problem
OpenMU Season 6 Ep3 has no mechanism to apply different damage multipliers based on attacker/defender class combinations. This leads to known imbalances:
- Dark Wizard burst is disproportionately high against all classes (ENE÷4 max damage, fragile by design but too punishing)
- Magic Gladiator has no resistance to wizardry despite having medium defense
- Fairy Elf is near-useless in 1v1 PvP (defense AGI÷10 vs DK's AGI÷3)

## Goal
Add a per-class damage receive reduction system for PvP that allows fine-tuned matchup balancing without touching base stat formulas.

## Approach
Add `DamageReceiveFromX` attributes to each character class (one per attacker class). In `CalculateDamageAsync`, detect the attacker's class and multiply final PvP damage by the defender's corresponding attribute.

This mirrors the existing `DamageReceiveDecrement` pattern already in the codebase, scoped to specific attacker classes.

Default value for all multipliers: `1.0` (no change). Admins tune per-matchup values in the admin panel.

---

## Files Changed

### 1. `src/GameLogic/Attributes/Stats.cs`
Add 6 new `AttributeDefinition` properties — one per attacker class:

```csharp
public static AttributeDefinition DamageReceiveFromDarkKnightDecrement { get; }
public static AttributeDefinition DamageReceiveFromDarkWizardDecrement { get; }
public static AttributeDefinition DamageReceiveFromFairyElfDecrement   { get; }
public static AttributeDefinition DamageReceiveFromMagicGladiatorDecrement { get; }
public static AttributeDefinition DamageReceiveFromDarkLordDecrement   { get; }
public static AttributeDefinition DamageReceiveFromRageFighterDecrement { get; }
```

Each needs a unique `Guid`.

---

### 2. `src/GameLogic/AttackableExtensions.cs`
In `CalculateDamageAsync`, after the existing `DamageReceiveDecrement` line (line ~205), add:

```csharp
if (isPvp && attackerPlayer is not null && dmg > 1)
{
    var classReductionStat = GetClassDamageReceiveStat(attackerPlayer);
    if (classReductionStat is not null)
        dmg = (int)(dmg * defender.Attributes[classReductionStat]);
}
```

Add private helper `GetClassDamageReceiveStat(Player attacker)` that maps `CharacterClass.Number` to the correct `AttributeDefinition` using `CharacterClassNumber` enum values:
- DarkKnight / BladeKnight / BladeMaster → `DamageReceiveFromDarkKnightDecrement`
- DarkWizard / SoulMaster / GrandMaster → `DamageReceiveFromDarkWizardDecrement`
- FairyElf / MuseElf / HighElf → `DamageReceiveFromFairyElfDecrement`
- MagicGladiator / DuelMaster → `DamageReceiveFromMagicGladiatorDecrement`
- DarkLord / LordEmperor → `DamageReceiveFromDarkLordDecrement`
- RageFighter / FistMaster → `DamageReceiveFromRageFighterDecrement`

---

### 3. `src/Persistence/Initialization/CharacterClasses/*.cs`
In each `CreateX()` method, add `BaseAttributeValues` entries for all 6 new attributes with default `1.0f`:

```csharp
result.BaseAttributeValues.Add(this.CreateConstValueAttribute(1.0f, Stats.DamageReceiveFromDarkKnightDecrement));
result.BaseAttributeValues.Add(this.CreateConstValueAttribute(1.0f, Stats.DamageReceiveFromDarkWizardDecrement));
// ... etc for all 6
```

This ensures fresh database installs get the attributes with neutral values.

---

### 4. `src/Persistence/Initialization/Updates/UpdateVersion.cs`
Add next version entry (currently last is `83`):

```csharp
/// <summary>
/// The version of the <see cref="AddPvpClassMultipliersUpdatePlugIn"/>.
/// </summary>
AddPvpClassMultipliersSeason6 = 84,
```

---

### 5. `src/Persistence/Initialization/Updates/AddPvpClassMultipliersUpdatePlugIn.cs`
New update plugin — applies the change to existing databases.

Responsibilities:
1. Register the 6 new `AttributeDefinition`s in `gameConfiguration.Attributes` (using `AddStatIfNotExists`)
2. For each `CharacterClass` in `gameConfiguration.CharacterClasses`, add `BaseAttributeValue` entries for all 6 new stats with default `1.0f` (only if not already present)

`DataInitializationKey`: `VersionSeasonSix.DataInitialization.Id`
`IsMandatory`: `true`
`CreatedAt`: date of implementation

---

## Default Multiplier Values
All start at `1.0` (no change from current behavior). Suggested starting tuning after testing:

| Defender → | vs DK | vs DW | vs Elf | vs MG | vs DL | vs RF |
|---|---|---|---|---|---|---|
| Dark Knight | 1.0 | 1.0 | 1.0 | 1.0 | 1.0 | 1.0 |
| Dark Wizard | 1.0 | 1.0 | 1.0 | 1.0 | 1.0 | 1.0 |
| Fairy Elf | 1.0 | 1.0 | 1.0 | 1.0 | 1.0 | 1.0 |
| Magic Gladiator | 1.0 | **0.75** | 1.0 | 1.0 | 1.0 | 1.0 |
| Dark Lord | 1.0 | 1.0 | 1.0 | 1.0 | 1.0 | 1.0 |
| Rage Fighter | 1.0 | 1.0 | 1.0 | 1.0 | 1.0 | 1.0 |

> MG vs DW starts at 0.75 as the most well-documented imbalance. All others start neutral and are tuned via admin panel after in-game testing.

---

## What this does NOT do
- Does not change base stat formulas (STR, AGI, VIT, ENE scaling)
- Does not affect PvE damage in any way (`isPvp` guard)
- Does not affect Raven/pet damage (AttackerSurrogate is not a Player)
- Does not require any client changes

## Out of scope
- Per-skill-type multipliers (e.g. "DW takes less from physical only")
- Admin panel UI for the matrix (values are tuned directly in DB via admin panel attribute editor)
