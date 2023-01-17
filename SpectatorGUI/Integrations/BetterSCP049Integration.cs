using PluginAPI.Core;

namespace Mistaken.SpectatorGUI.Integrations;

internal sealed class BetterSCP049Integration
{
    public static bool Enabled = false;

    public static bool IsCuffed(Player player)
    {
        return false;
        // return Mistaken.BetterSCP.SCP049.Commands.DisarmCommand.DisarmedScps.ContainsValue(player);
    }
}
