using HarmonyLib;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;

namespace Mistaken.SpectatorGUI;

internal sealed class Plugin
{
    public static Plugin Instance { get; private set; }

    [PluginConfig]
    public Config Config;

    public Translation Translation;

    [PluginPriority(LoadPriority.Medium)]
    [PluginEntryPoint("Spectator GUI", "1.0.0", "", "Mistaken Devs")]
    private void Load()
    {
        Instance = this;
        _harmony.PatchAll();
        Translation = new();

        new SpectatorInfoHandler();
    }

    [PluginUnload]
    private void Unload()
    {
        _harmony.UnpatchAll();
    }

    private static readonly Harmony _harmony = new("mistaken.spectatrgui");
}
