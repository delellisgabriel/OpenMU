// <copyright file="CustomServerRatesUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Sets custom server rates: experience (1000x), master experience (1000x),
/// excellent item drop delta (10) and maximum item option level drop (4).
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("A1B2C3D4-0001-0001-0001-000000000001")]
public class CustomServerRatesUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>The plug in name.</summary>
    internal const string PlugInName = "Custom Server Rates";

    /// <summary>The plug in description.</summary>
    internal const string PlugInDescription = "Sets custom server rates: 1000x experience, excellent item drop delta 10, maximum item option level drop 4.";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.CustomServerRates;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 06, 05, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        gameConfiguration.ExperienceRate = 1000f;
        gameConfiguration.MasterExperienceRate = 1000f;
        gameConfiguration.ExcellentItemDropLevelDelta = 10;
        gameConfiguration.MaximumItemOptionLevelDrop = 4;

        return ValueTask.CompletedTask;
    }
}
