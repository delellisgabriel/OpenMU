# Verification Guide

How to confirm each custom update applied correctly after running **admin panel → Configuration → Updates**.

---

## 1. Check that updates ran

**Admin panel → Configuration → Updates**

All of these should appear with a green checkmark / installed timestamp:

| Version | Name |
|---|---|
| v84 | Add PvP Class vs Class Damage Multipliers |
| v100 | Custom Server Rates |
| v101 | Custom Jewel Rates |

---

## 2. Verify v100 — Server Rates

**Admin panel → Configuration → Game Configuration**

| Field | Expected value |
|---|---|
| ExperienceRate | 1000 |
| MasterExperienceRate | 1000 |
| ExcellentItemDropLevelDelta | 10 |
| MaximumItemOptionLevelDrop | 4 |

---

## 3. Verify v101 — Soul Jewel Rates

**Admin panel → Configuration → Plugin Configurations → SoulJewelConsumeHandlerPlugIn**

| Field | Expected value |
|---|---|
| SuccessRatePercentage | 70 |
| SuccessRateBonusWithLuckPercentage | 30 |
| ResetToLevel0WhenFailMinLevel | 8 |

---

## 4. Verify v84 — PvP Class Multipliers

**Admin panel → Configuration → Character Classes → [any class] → Base Attribute Values**

All 6 of these should be present at `1.0`:

- PvP Damage Receive From Dark Knight Multiplier
- PvP Damage Receive From Dark Wizard Multiplier
- PvP Damage Receive From Fairy Elf Multiplier
- PvP Damage Receive From Magic Gladiator Multiplier
- PvP Damage Receive From Dark Lord Multiplier
- PvP Damage Receive From Rage Fighter Multiplier

---

## 5. Verify via database (optional, more direct)

```powershell
# Server rates
docker exec database psql -U postgres -d openmu -c 'SELECT "ExperienceRate", "MasterExperienceRate", "ExcellentItemDropLevelDelta", "MaximumItemOptionLevelDrop" FROM config."GameConfiguration"'

# Soul Jewel config
docker exec database psql -U postgres -d openmu -c "SELECT \"CustomConfiguration\" FROM config.\"PlugInConfiguration\" WHERE \"TypeId\" = 'a76cda49-1c56-401a-96d1-294d9a68a7b9'"

# PvP attributes on a character class (Dark Knight = number 4)
docker exec database psql -U postgres -d openmu -c 'SELECT ad."Designation", cva."Value" FROM config."ConstValueAttribute" cva JOIN config."AttributeDefinition" ad ON ad."Id" = cva."DefinitionId" JOIN config."CharacterClass" cc ON cc."Id" = cva."CharacterClassId" WHERE cc."Number" = 4 AND ad."Designation" LIKE $$%PvP%$$'
```

---

## 6. After tuning PvP values

Once you change a multiplier in the admin panel (e.g. MG's DamageReceiveFromDarkWizard to 0.75), verify it took effect:

```powershell
docker exec database psql -U postgres -d openmu -c 'SELECT ad."Designation", cva."Value" FROM config."ConstValueAttribute" cva JOIN config."AttributeDefinition" ad ON ad."Id" = cva."DefinitionId" JOIN config."CharacterClass" cc ON cc."Id" = cva."CharacterClassId" WHERE cc."Number" = 12 AND ad."Designation" LIKE $$%PvP%$$'
```

MagicGladiator is class number `12`. You should see `0.75` next to `PvP Damage Receive From Dark Wizard Multiplier`.
