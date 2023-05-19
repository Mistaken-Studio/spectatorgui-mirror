using CommandSystem;
using InventorySystem.Disarming;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.MicroHID;
using MEC;
using Mistaken.PseudoGUI;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;
using PlayerRoles.Spectating;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using PluginAPI.Events;
using Respawning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Mistaken.SpectatorGUI;

internal sealed class SpectatorInfoHandler
{
    public static Func<Player, string> AdminDescriptor { get; set; } = (player) =>
    {
        return $"<br>Id: {player.PlayerId}<br>Cuffed: {IsCuffed(player)}";
    };

    private static bool IsCuffed(Player player)
    {
        if (player.Team != Team.SCPs)
            return player.ReferenceHub.inventory.IsDisarmed();

        return false;
    }

    public SpectatorInfoHandler()
    {
        _active = true;
        EventManager.RegisterEvents(this);
        Task.Run(UpdateTask);
    }

    ~SpectatorInfoHandler()
    {
        _active = false;
        EventManager.UnregisterEvents(this);
    }

    private static readonly Dictionary<int, string> _deathMessages = new();
    private static readonly Dictionary<ReferenceHub, RoleTypeId> _spawnQueue = new();
    private static readonly RespawnTokensManager.TokenCounter _counterCI = RespawnTokensManager.Counters.First(x => x.Team == SpawnableTeamType.ChaosInsurgency);
    private static readonly RespawnTokensManager.TokenCounter _counterNTF = RespawnTokensManager.Counters.First(x => x.Team == SpawnableTeamType.NineTailedFox);
    private static MapGeneration.Distributors.Scp079Generator _cache_nearestGenerator;
    private static int _seed = -1;

    private static float Dynamic_maxRespawnCI => Mathf.Min(_counterCI.Handler.MaxWaveSize, _counterCI.Amount == 0 ? 5 : _counterCI.Amount);

    private static float Dynamic_maxRespawnMTF => Mathf.Min(_counterNTF.Handler.MaxWaveSize, _counterNTF.Amount);

    private static T GetSpectatedPlayer<T>(T player) where T : Player
        => player.ReferenceHub.roleManager.CurrentRole is SpectatorRole spectator ? Player.Get<T>(spectator.SyncedSpectatedNetId) : null;

    private static void ShuffleList<T>(IList<T> list, int seed)
    {
        System.Random random = new(seed);
        int i = list.Count;
        while (i > 1)
        {
            i--;
            int index = random.Next(i + 1);
            (list[i], list[index]) = (list[index], list[i]);
        }
    }

    private bool _active;
    private bool _roundStarted;

    private async Task UpdateTask()
    {
        while (_active)
        {
            try
            {
                await Task.Delay(1000);

                if (!_roundStarted)
                    continue;

                var spectators = Player.GetPlayers().Where(x => x.Role == RoleTypeId.Spectator || x.Role == RoleTypeId.Overwatch).ToArray();
                var spectatorsCount = spectators.Length;

                if (spectatorsCount == 0)
                    continue;

                try
                {
                    var start = DateTime.UtcNow;
                    var respawnManager = RespawnManager.Singleton;

                    var RespawnList = ReferenceHub.AllHubs.Where(x => x.roleManager.CurrentRole is SpectatorRole specRole && specRole.ReadyToRespawn).ToList();
                    var RespawnCount = RespawnList.Count;

                    var respawningCI = Math.Min(Dynamic_maxRespawnCI, RespawnCount);
                    var notrespawningCI = RespawnCount - respawningCI;

                    var respawningMTF = Math.Min(Dynamic_maxRespawnMTF, RespawnCount);
                    var notrespawningMTF = RespawnCount - respawningMTF;

                    var ttr = Mathf.RoundToInt(respawnManager._timeForNextSequence - (float)respawnManager._stopwatch.Elapsed.TotalSeconds);

                    _spawnQueue.Clear();
                    RespawnPatch.PlayersToSpawn.Clear();
                    if (respawnManager._curSequence == RespawnManager.RespawnSequencePhase.PlayingEntryAnimations)
                    {
                        if (RespawnManager.SpawnableTeams.TryGetValue(respawnManager.NextKnownTeam, out SpawnableTeamHandlerBase spawnableTeam))
                        {
                            int maxWaveSize = spawnableTeam.MaxWaveSize;
                            int num = RespawnCount;
                            if (num > maxWaveSize)
                            {
                                RespawnList.RemoveRange(maxWaveSize, num - maxWaveSize);
                                num = maxWaveSize;
                            }

                            if (_seed == -1)
                                _seed = UnityEngine.Random.Range(0, int.MaxValue);

                            if (RespawnManager.Singleton._prioritySpawn)
                                RespawnList = RespawnList.OrderByDescending(x => x.roleManager.CurrentRole.ActiveTime).ToList();
                            else
                                ShuffleList(RespawnList, _seed);

                            RespawnPatch.PlayersToSpawn = RespawnList;
                            Queue<RoleTypeId> queue = new();
                            spawnableTeam.GenerateQueue(queue, RespawnCount);

                            foreach (var item in RespawnList)
                                _spawnQueue.Add(item, queue.Dequeue());
                        }
                    }

                    string message = PrepareInfoText(spectatorsCount, out string adminMessage);
                    string respawnWaiting = InformRespawnWaiting(ttr);
                    string respawnMsg;

                    foreach (var player in spectators)
                    {
                        if (!_deathMessages.TryGetValue(player.PlayerId, out string outputMessage))
                            outputMessage = string.Empty;

                        if (respawnManager._curSequence == RespawnManager.RespawnSequencePhase.PlayingEntryAnimations)
                        {
                            if (respawnManager.NextKnownTeam == SpawnableTeamType.ChaosInsurgency)
                                respawnMsg = InformRespawnCI(ttr, respawningCI, notrespawningCI, _spawnQueue.ContainsKey(player.ReferenceHub) ? _spawnQueue[player.ReferenceHub] : RoleTypeId.None);
                            else if (respawnManager.NextKnownTeam == SpawnableTeamType.NineTailedFox)
                                respawnMsg = InformRespawnMTF(ttr, respawningMTF, notrespawningMTF, _spawnQueue.ContainsKey(player.ReferenceHub) ? _spawnQueue[player.ReferenceHub] : RoleTypeId.None, _spawnQueue.FirstOrDefault(i => i.Value == RoleTypeId.NtfCaptain).Key.nicknameSync?.DisplayName ?? "UNKNOWN");
                            else
                                respawnMsg = InformRespawnNone(ttr);
                        }
                        else
                            respawnMsg = respawnWaiting;

                        outputMessage += InformTTR(message, respawnMsg, false, adminMessage);
                        outputMessage += "<br><br>" + InformSpectating(GetSpectatedPlayer(player), false);

                        if (player.IsAlive)
                            player.SetGUI("specInfo", PseudoGUIPosition.MIDDLE, null);
                        else
                            player.SetGUI("specInfo", PseudoGUIPosition.MIDDLE, outputMessage);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                    Log.Error(ex.StackTrace);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                Log.Error(ex.StackTrace);
            }
        }
    }

    [PluginEvent(ServerEventType.PlayerChangeRole)]
    private void OnPlayerChangeRole(Player player, PlayerRoleBase oldRole, RoleTypeId newRole, RoleChangeReason reason)
    {
        if (newRole != RoleTypeId.Spectator && newRole != RoleTypeId.Overwatch)
        {
            player.SetGUI("specInfo", PseudoGUIPosition.MIDDLE, null);
            Timing.CallDelayed(1, () => player.SetGUI("specInfo", PseudoGUIPosition.MIDDLE, null));
        }
    }

    [PluginEvent(ServerEventType.TeamRespawn)]
    private void OnTeamRespawn(SpawnableTeamType team)
    {
        if (RespawnManager.SpawnableTeams.TryGetValue(team, out var spawnableTeam) || team == SpawnableTeamType.None)
        {
            float tickets = _counterCI.Amount;
            if (tickets - 1f < 0)
            {
                tickets = 5;
                RespawnTokensManager.GrantTokens(SpawnableTeamType.ChaosInsurgency, 5);
            }
        }

        Timing.CallDelayed(20, () => _seed = -1);
    }

    [PluginEvent(ServerEventType.RoundRestart)]
    private void OnRoundRestart()
    {
        _seed = -1;
        _roundStarted = false;
    }

    [PluginEvent(ServerEventType.RoundStart)]
    private void OnRoundStart()
    {
        Timing.RunCoroutine(UpdateCache(), nameof(UpdateCache));
        _roundStarted = true;
    }

    [PluginEvent(ServerEventType.WaitingForPlayers)]
    private void OnWaitingForPlayers()
    {
    }

    private IEnumerator<float> UpdateCache()
    {
        yield return Timing.WaitForSeconds(1);
        while (Round.IsRoundStarted)
        {
            yield return Timing.WaitForSeconds(5);

            _cache_nearestGenerator = null;
            foreach (var generator in Scp079Recontainer.AllGenerators)
            {
                if (generator.Activating)
                {
                    if ((_cache_nearestGenerator?.Network_syncTime ?? float.MaxValue) > generator.Network_syncTime)
                        _cache_nearestGenerator = generator;
                }
            }
        }
    }

    private string InformTTR(string preparedText, string respawnMsg, bool admin, string adminMessage)
    {
        string masterAdminMessage = string.Empty;
        if (Round.IsLocked)
            masterAdminMessage = "[<color=yellow>ROUND LOCK <b>ACTIVE</b></color>]";

        return preparedText.Replace("{respawnMsg}", respawnMsg) + (admin ? $"<br>{adminMessage.Replace("{masterAdminMessage}", masterAdminMessage)}" : string.Empty);
    }

    private string PrepareInfoText(int spectators, out string adminMessage)
    {
        var roundTimeString = string.Format(Plugin.Translations.RoundInfo, Round.Duration.Minutes.ToString("00"), Round.Duration.Seconds.ToString("00"));
        var specatorString = spectators < 2 ? Plugin.Translations.OnlySpectatorInfo : string.Format(Plugin.Translations.SpectatorInfo, spectators - 1);
        var playersString = string.Format(Plugin.Translations.PlayersInfo, ServerConsole._playersAmount, CustomNetworkManager.slots);
        adminMessage = string.Format(Plugin.Translations.AdminInfo, "{masterAdminMessage}", Mathf.RoundToInt(_counterNTF.Amount), Mathf.RoundToInt(_counterCI.Amount));
        return $@"<size=50%>{roundTimeString}   |   {playersString}</size><br>{specatorString}<br>{{respawnMsg}}<br><br><br><br><br><br><br><br><br><br><br><br>";
    }

    private string InformSpectating(Player player, bool admin)
    {
        if (player is null || player.IsAlive || player.GameObject == null)
            return string.Empty;

        var roleName = $"<color=#{player.RoleColor}>{player.Role}</color>";
        string tor = $"{player.DisplayNickname} is <color=yellow>playing</color> as {roleName}";

        var currentItem = player.CurrentItem;
        if (currentItem is not null)
        {
            tor += $"<br> and is holding {currentItem.ItemTypeId}";
            if (currentItem is Firearm firearm)
                tor += $" (<color=yellow>{firearm.Status.Ammo}</color>/<color=yellow>{firearm.AmmoManagerModule.MaxAmmo}</color>), ammo: <color=yellow>{(player.AmmoBag.TryGetValue(firearm.AmmoType, out var ammo) ? ammo : 0)}</color>";
            else if (currentItem is MicroHIDItem microHid)
                tor += $" with <color=yellow>{Math.Floor(microHid.RemainingEnergy * 100)}%</color>";
        }

        if (admin)
            tor += AdminDescriptor(player);

        return "<size=50%>" + tor + "</size>";
    }

    private string InformRespawnWaiting(float ttr)
    {
        return string.Format(Plugin.Translations.RespawnInfo, ((ttr - (ttr % 60)) / 60).ToString("00"), (ttr % 60).ToString("00"));
    }

    private string InformRespawnNone(float ttr)
    {
        return string.Format(Plugin.Translations.RespawnNone, (ttr % 60).ToString("00"));
    }

    private string InformRespawnMTF(float ttr, float respawningMTF, float notrespawningMTF, RoleTypeId expectedRole, string commander)
    {
        string roleString = expectedRole == RoleTypeId.None ? string.Format(Plugin.Translations.RespawnMTFWillNotRespawn, commander) : string.Format(Plugin.Translations.RespawnMTFWillRespawn);
        switch (expectedRole)
        {
            case RoleTypeId.NtfPrivate:
                roleString += string.Format(Plugin.Translations.RespawnMTFWillRespawnPrivate, commander);
                break;

            case RoleTypeId.NtfSergeant:
                roleString += string.Format(Plugin.Translations.RespawnMTFWillRespawnSergeant, commander);
                break;

            case RoleTypeId.NtfCaptain:
                roleString += string.Format(Plugin.Translations.RespawnMTFWillRespawnCaptain);
                break;
        }

        return string.Format(Plugin.Translations.RespawnMTFRespawn, (ttr % 60).ToString("00"), respawningMTF, notrespawningMTF, roleString);
    }

    private string InformRespawnCI(float ttr, float respawningCI, float notrespawningCI, RoleTypeId expectedRole)
    {
        string roleString = expectedRole == RoleTypeId.None ? string.Format(Plugin.Translations.RespawnCIWillNotRespawn) : string.Format(Plugin.Translations.RespawnCIWillRespawn);
        switch (expectedRole)
        {
            case RoleTypeId.ChaosRifleman:
                roleString += string.Format(Plugin.Translations.RespawnCIWillRespawnRifleman);
                break;

            case RoleTypeId.ChaosRepressor:
                roleString += string.Format(Plugin.Translations.RespawnCIWillRespawnRepressor);
                break;

            case RoleTypeId.ChaosMarauder:
                roleString += string.Format(Plugin.Translations.RespawnCIWillRespawnMarauder);
                break;
        }

        return string.Format(Plugin.Translations.RespawnCIRespawn, (ttr % 60).ToString("00"), respawningCI, notrespawningCI, roleString);
    }
}

[CommandHandler(typeof(RemoteAdminCommandHandler))]
internal sealed class SetTimeToRespawn : ICommand
{
    public string Command => "sttr";

    public string[] Aliases => Array.Empty<string>();

    public string Description => "Set's time to respawn";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (arguments.Count == 0)
        {
            response = "You must provide a number";
            return false;
        }

        if (!int.TryParse(arguments.At(0), out var time))
        {
            response = "You must provide a number";
            return false;
        }

        RespawnManager.Singleton._timeForNextSequence = time;
        response = "Done";
        return true;
    }
}
