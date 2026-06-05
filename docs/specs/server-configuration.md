# Spec: Custom Server Configuration

## Status
`ready-to-implement`

## Problem
Server configuration changes made via the admin panel are stored only in the PostgreSQL database. They are lost on DB reset and not reproduced when someone clones the fork and runs it fresh.

## Goal
Codify all custom server settings into update plugins so the configuration is:
- Reproducible on any fresh install
- Version-controlled alongside the code
- Applied automatically to existing databases via the update system

---

## Changes to Codify

### GameConfiguration
| Field | Default | Custom value | Reason |
|---|---|---|---|
| `ExperienceRate` | 1 | 1000 | 1000x experience for fast progression |
| `MasterExperienceRate` | 1 | 1000 | Same for master levels |
| `ExcellentItemDropLevelDelta` | 25 | 10 | Better excellent items from lower-level monsters |
| `MaximumItemOptionLevelDrop` | 3 | 4 | Items can drop with max option level |

### Soul Jewel Plugin (TypeId: `a76cda49-1c56-401a-96d1-294d9a68a7b9`)
| Field | Default | Custom value | Reason |
|---|---|---|---|
| `SuccessRatePercentage` | 50 | 70 | More forgiving upgrade success rate |
| `SuccessRateBonusWithLuckPercentage` | 25 | 30 | Slightly better luck bonus |
| `ResetToLevel0WhenFailMinLevel` | 7 | 8 | Items only drop to +0 on fail at +8 (not +7) |

---

## Files Changed

### 1. `src/Persistence/Initialization/Updates/UpdateVersion.cs`
Add two new entries starting at 100 to avoid upstream conflicts:

```csharp
CustomServerRates = 100,
CustomJewelRates = 101,
```

### 2. `src/Persistence/Initialization/Updates/CustomServerRatesUpdatePlugIn.cs`
New update plugin (version 100) — updates `GameConfiguration` fields.

In `ApplyAsync`:
```csharp
gameConfiguration.ExperienceRate = 1000;
gameConfiguration.MasterExperienceRate = 1000;
gameConfiguration.ExcellentItemDropLevelDelta = 10;
gameConfiguration.MaximumItemOptionLevelDrop = 4;
```

`DataInitializationKey`: `VersionSeasonSix.DataInitialization.Id`
`IsMandatory`: `true`

### 3. `src/Persistence/Initialization/Updates/CustomJewelRatesUpdatePlugIn.cs`
New update plugin (version 101) — updates the Soul Jewel plugin configuration.

In `ApplyAsync`:
- Find the `PlugInConfiguration` entry with `TypeId == Guid("a76cda49-1c56-401a-96d1-294d9a68a7b9")`
- Deserialize its `CustomConfiguration` JSON
- Update the fields
- Re-serialize and save back

`DataInitializationKey`: `VersionSeasonSix.DataInitialization.Id`
`IsMandatory`: `true`

### 4. `src/Persistence/Initialization/VersionSeasonSix/GameConfigurationInitializer.cs`
Update the initial seed values so fresh installs start with the custom values directly, without needing to run the update plugins.

---

## Version Numbering Convention
Custom versions start at **100** to avoid conflicts with upstream (currently at 83).
Upstream versions: 1–99
Custom versions: 100+
