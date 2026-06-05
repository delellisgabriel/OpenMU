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

---

## 7. In-game PvP damage testing

Use two client instances with existing test accounts to measure live damage numbers.

### Class numbers reference
| # | Class |
|---|---|
| 0 | Dark Wizard |
| 4 | Dark Knight |
| 8 | Fairy Elf |
| 12 | Magic Gladiator |
| 16 | Dark Lord |
| 24 | Rage Fighter |

### Test accounts available
| Account | Characters | Level |
|---|---|---|
| `testgm` | testgmDk, testgmDw, testgmElf, testgmMg, testgmDl | 400 |
| `testgm2` | testgm2Rf, testgm2Sum | 400 |
| `test400` | test400Dk, test400Dw, test400Elf, test400Mg, test400Dl | 400 |
| `test0–test9` | one of each class | 1–90 |

All passwords = username.

### Setup
1. Open `MUnique.OpenMU.ClientLauncher.exe` **twice** — MU supports multiple instances
2. **Window 1 (attacker):** login as `testgm`, pick your attacker class
3. **Window 2 (defender):** login as `test400`, pick your target class — **do not move**
4. Both go to **Lorencia**, right-click defender → **Duel request**, defender accepts
5. Attacker hits the defender and reads floating damage numbers

### How to verify a multiplier change
1. Note baseline damage at `1.0`
2. Admin panel → **Configuration → Character Classes → [defender class] → Base Attribute Values**
3. Change `PvP Damage Receive From [attacker class] Multiplier` to e.g. `0.75`
4. Hit again — damage should drop ~25%
5. Confirm via DB:
```powershell
# Replace 12 with the defender's class number
docker exec database psql -U postgres -d openmu -c 'SELECT ad."Designation", cva."Value" FROM config."ConstValueAttribute" cva JOIN config."AttributeDefinition" ad ON ad."Id" = cva."DefinitionId" JOIN config."CharacterClass" cc ON cc."Id" = cva."CharacterClassId" WHERE cc."Number" = 12 AND ad."Designation" LIKE $$%PvP%$$'
```
