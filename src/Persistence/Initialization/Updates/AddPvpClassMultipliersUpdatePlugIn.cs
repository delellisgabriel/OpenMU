// <copyright file="AddPvpClassMultipliersUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;
using static CharacterClasses.CharacterClassHelper;

/// <summary>
/// Adds per-attacker-class PvP damage receive multiplier attributes to all character classes.
/// All values default to 1.0 (neutral). Tune per-matchup via the admin panel.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("D4E5F6A7-0084-0084-0084-000000000084")]
public class AddPvpClassMultipliersUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>The plug in name.</summary>
    internal const string PlugInName = "Add PvP Class vs Class Damage Multipliers";

    /// <summary>The plug in description.</summary>
    internal const string PlugInDescription = "Adds per-attacker-class PvP damage receive multiplier attributes to all character classes. Defaults to 1.0 (no change).";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.AddPvpClassMultipliersSeason6;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 06, 05, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        // Register the 6 new attribute definitions in the game configuration.
        this.AddStatIfNotExists(context, gameConfiguration, Stats.DamageReceiveFromDarkKnightDecrement);
        this.AddStatIfNotExists(context, gameConfiguration, Stats.DamageReceiveFromDarkWizardDecrement);
        this.AddStatIfNotExists(context, gameConfiguration, Stats.DamageReceiveFromFairyElfDecrement);
        this.AddStatIfNotExists(context, gameConfiguration, Stats.DamageReceiveFromMagicGladiatorDecrement);
        this.AddStatIfNotExists(context, gameConfiguration, Stats.DamageReceiveFromDarkLordDecrement);
        this.AddStatIfNotExists(context, gameConfiguration, Stats.DamageReceiveFromRageFighterDecrement);

        // Add neutral default values to every character class.
        foreach (var characterClass in gameConfiguration.CharacterClasses)
        {
            AddIfMissing(context, gameConfiguration, characterClass, Stats.DamageReceiveFromDarkKnightDecrement);
            AddIfMissing(context, gameConfiguration, characterClass, Stats.DamageReceiveFromDarkWizardDecrement);
            AddIfMissing(context, gameConfiguration, characterClass, Stats.DamageReceiveFromFairyElfDecrement);
            AddIfMissing(context, gameConfiguration, characterClass, Stats.DamageReceiveFromMagicGladiatorDecrement);
            AddIfMissing(context, gameConfiguration, characterClass, Stats.DamageReceiveFromDarkLordDecrement);
            AddIfMissing(context, gameConfiguration, characterClass, Stats.DamageReceiveFromRageFighterDecrement);
        }

        return ValueTask.CompletedTask;
    }

    private static void AddIfMissing(IContext context, GameConfiguration gameConfiguration, CharacterClass characterClass, AttributeDefinition attribute)
    {
        if (characterClass.BaseAttributeValues.Any(a => a.Definition?.Id == attribute.Id))
        {
            return;
        }

        characterClass.BaseAttributeValues.Add(CreateConstValueAttribute(context, gameConfiguration, 1.0f, attribute));
    }
}
