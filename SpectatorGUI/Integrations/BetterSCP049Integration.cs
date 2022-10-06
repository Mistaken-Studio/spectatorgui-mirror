// -----------------------------------------------------------------------
// <copyright file="BetterSCP049Integration.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Exiled.API.Features;

namespace Mistaken.SpectatorGUI.Integrations
{
    // ReSharper disable once InconsistentNaming
    internal static class BetterSCP049Integration
    {
        public static bool Enabled { get; set; }

        public static bool IsCuffed(Player player)
        {
            return BetterSCP.SCP049.Commands.DisarmCommand.DisarmedScps.ContainsValue(player);
        }
    }
}