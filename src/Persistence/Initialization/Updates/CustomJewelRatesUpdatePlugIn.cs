// <copyright file="CustomJewelRatesUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Updates the Jewel of Soul plugin configuration:
/// success rate 70%, luck bonus 30%, reset to +0 only when failing on +8.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("A1B2C3D4-0002-0002-0002-000000000002")]
public class CustomJewelRatesUpdatePlugIn : UpdatePlugInBase
{
    private static readonly Guid SoulJewelPlugInTypeId = new("a76cda49-1c56-401a-96d1-294d9a68a7b9");

    /// <summary>The plug in name.</summary>
    internal const string PlugInName = "Custom Jewel Rates";

    /// <summary>The plug in description.</summary>
    internal const string PlugInDescription = "Updates Jewel of Soul: 70% success rate, 30% luck bonus, reset to +0 only on fail at +8.";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.CustomJewelRates;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 06, 05, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var plugInConfig = gameConfiguration.PlugInConfigurations
            .FirstOrDefault(p => p.TypeId == SoulJewelPlugInTypeId);

        if (plugInConfig is null)
        {
            return ValueTask.CompletedTask;
        }

        var config = plugInConfig.GetConfiguration<UpgradeItemLevelConfiguration>(null)
            ?? new UpgradeItemLevelConfiguration();

        config.SuccessRatePercentage = 70;
        config.SuccessRateBonusWithLuckPercentage = 30;
        config.ResetToLevel0WhenFailMinLevel = 8;

        plugInConfig.SetConfiguration(config, null);

        return ValueTask.CompletedTask;
    }
}
