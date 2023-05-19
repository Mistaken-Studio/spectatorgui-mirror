using HarmonyLib;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;

namespace Mistaken.SpectatorGUI;

internal sealed class Plugin
{
    public static Plugin Instance { get; private set; }

    [PluginConfig]
    public static Config Config;

    public static Translations Translations;

    [PluginPriority(LoadPriority.Medium)]
    [PluginEntryPoint("Spectator GUI", "1.0.0", "Spectator GUI", "Mistaken Devs")]
    private void Load()
    {
        Instance = this;
        _harmony.PatchAll();
        Translations = new();

        new SpectatorInfoHandler();
    }

    [PluginUnload]
    private void Unload()
    {
        _harmony.UnpatchAll();
    }

    private static readonly Harmony _harmony = new("mistaken.spectatorgui");
}
