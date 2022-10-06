using System.Linq;
using Exiled.API.Features;

namespace Mistaken.SpectatorGUI.Integrations
{
    public class BetterSCP049Integration
    {
        /// <summary>
        /// Checks if the integration is enabled.
        /// </summary>
        public static bool Enabled = false;

        /// <summary>
        /// Checks if the player is cuffed.
        /// </summary>
        /// <param name="player">player.</param>
        /// <returns><see cref="bool"/>.</returns>
        public static bool IsCuffed(Player player)
        {
            return Mistaken.BetterSCP.SCP049.Commands.DisarmCommand.DisarmedScps.ContainsValue(player);
        }
    }
}