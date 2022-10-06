using System.Linq;
using Exiled.API.Features;

namespace Mistaken.SpectatorGUI.Integrations
{
    public class BetterSCP049Integration
    {
        public static bool Enabled = false;

        public BetterSCP049Integration()
        {
            Events.Handlers.CustomEvents.LoadedPlugins -= this.CustomEvents_LoadedPlugins;
        }

        ~BetterSCP049Integration()
        {
            Events.Handlers.CustomEvents.LoadedPlugins -= this.CustomEvents_LoadedPlugins;
        }

        private void CustomEvents_LoadedPlugins()
        {
            if(Exiled.Loader.Loader.Plugins.Any(x => x.Name == "BetterSCP-SCP049" && x.Config.IsEnabled))
                Enabled = true;
        }

        public static bool IsCuffed(Player player)
        {
            return Mistaken.BetterSCP.SCP049.Commands.DisarmCommand.DisarmedScps.ContainsValue(player);
        }
    }
}