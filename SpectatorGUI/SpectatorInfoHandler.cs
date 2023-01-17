using InventorySystem.Disarming;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.MicroHID;
using MEC;
using Mistaken.PseudoGUI;
using Mistaken.SpectatorGUI.Integrations;
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
    /// <summary>
    /// Dictionary containing Custom class descryptors to describe classes.
    /// </summary>
    public static readonly Dictionary<RoleTypeId, Func<Player, string>> CustomClassDescriptors = new();

    /*static SpectatorInfoHandler()
    {
        CustomClassDescriptors[RoleTypeId.Scp096] = (player) =>
        {
            var scp = player.RoleBase as Scp096Role;
            if (!scp.SubroutineModule.TryGetSubroutine<Scp096RageManager>(out var rageManager))
                return string.Empty;

            var rageText = $"<br> Rage left: <color=yellow>{Mathf.RoundToInt(rageManager.EnragedTimeLeft)}</color>s<br>Targets: <color=yellow>{Mathf.RoundToInt(rageManager._targetsTracker.Targets.Count)}</color>";
            var cooldownText = $"<br> Cooldown left: <color=yellow>{Mathf.RoundToInt(rageManager.RemainingEnrageCooldown)}</color>s";

            switch (scp.StateController.RageState)
            {
                case Scp096RageState.Distressed:
                case Scp096RageState.Enraged:
                    return rageText;

                case Scp096RageState.Docile:
                case Scp096RageState.Calming:
                    return scp.RemainingEnrageCooldown != 0 ? cooldownText : string.Empty;

                default:
                    return string.Empty;
            }
        };
    }*/

    /// <summary>
    /// Gets or sets info about spectated player if spectating player is admin.
    /// </summary>
    public static Func<Player, string> AdminDescriptor { get; set; } = (player) =>
    {
        return $"<br>Id: {player.PlayerId}<br>Cuffed: {IsCuffed(player)}";
    };

    private static bool IsCuffed(Player player)
    {
        if (player.Team != Team.SCPs)
            return player.ReferenceHub.inventory.IsDisarmed();

        if (BetterSCP049Integration.Enabled && player.Role == RoleTypeId.Scp049)
            return BetterSCP049Integration.IsCuffed(player);

        return false;
    }

    public SpectatorInfoHandler()
    {
        this._active = true;
        EventManager.RegisterEvents(this);
        Task.Run(UpdateTask);
    }

    ~SpectatorInfoHandler()
    {
        this._active = false;
        EventManager.UnregisterEvents(this);
    }

    private static readonly Dictionary<int, string> _deathMessages = new();
    // private static bool _is106 = false;
    private static RespawnTokensManager.TokenCounter _counterCI;
    private static RespawnTokensManager.TokenCounter _counterNTF;
    private static MapGeneration.Distributors.Scp079Generator _cache_nearestGenerator;
    private static int _respawnQueueSeed = -1;

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

    private readonly Dictionary<int, (Player Player, RoleTypeId Role)> spawnQueue = new();
    private bool _active;
    private bool _roundStarted;

    private async Task UpdateTask()
    {
        while (this._active)
        {
            try
            {
                await Task.Delay(1000);

                if (!this._roundStarted)
                    continue;

                var spectators = Player.GetPlayers().Where(x => x.Role == RoleTypeId.Spectator || x.Role == RoleTypeId.Overwatch).ToArray();
                var spectatorsCount = spectators.Length;

                if (spectatorsCount == 0)
                    continue;

                try
                {
                    var start = DateTime.UtcNow;
                    var respawnManager = RespawnManager.Singleton;

                    var toRespawnList = Player.GetPlayers().Where(x => x.Role == RoleTypeId.Spectator && !x.IsOverwatchEnabled).ToArray();
                    var toRespawn = toRespawnList.Count();

                    var respawningCI = Math.Min(Dynamic_maxRespawnCI, toRespawn);
                    var notrespawningCI = toRespawn - respawningCI;

                    var respawningMTF = Math.Min(Dynamic_maxRespawnMTF, toRespawn);
                    var notrespawningMTF = toRespawn - respawningMTF;

                    var ttr = Mathf.RoundToInt(respawnManager._timeForNextSequence - (float)respawnManager._stopwatch.Elapsed.TotalSeconds);

                    this.spawnQueue.Clear();
                    if (respawnManager._curSequence == RespawnManager.RespawnSequencePhase.PlayingEntryAnimations)
                    {
                        if (RespawnManager.SpawnableTeams.TryGetValue(respawnManager.NextKnownTeam, out SpawnableTeamHandlerBase spawnableTeam))
                        {
                            List<Player> list = toRespawnList.OrderBy(rh => rh.RoleBase.ActiveTime).ToList();
                            int maxRespawnablePlayers = Mathf.RoundToInt(respawnManager.NextKnownTeam == SpawnableTeamType.ChaosInsurgency ? Dynamic_maxRespawnCI : Dynamic_maxRespawnMTF);
                            maxRespawnablePlayers = Math.Max(maxRespawnablePlayers, 0);

                            while (list.Count > maxRespawnablePlayers)
                                list.RemoveAt(list.Count - 1);

                            // foreach (var player in list)
                            //     this.spawnQueue.Add(player.Id, (player, RoleTypeId.Spectator));
                            if (_respawnQueueSeed == -1)
                                _respawnQueueSeed = UnityEngine.Random.Range(0, 10000);

                            ShuffleList(list, _respawnQueueSeed);
                            Queue<RoleTypeId> queue = new();
                            spawnableTeam.GenerateQueue(queue, list.Count);
                            foreach (var player in list)
                            {
                                try
                                {
                                    this.spawnQueue.Add(player.PlayerId, (player, queue.Dequeue()));
                                }
                                catch
                                {
                                }
                            }

                            NorthwoodLib.Pools.ListPool<Player>.Shared.Return(list);
                        }
                    }

                    string message = this.PrepareInfoText(spectatorsCount, out string adminMessage);
                    string respawnWaiting = this.InformRespawnWaiting(ttr);
                    string respawnMsg;

                    foreach (var player in spectators)
                    {
                        if (!_deathMessages.TryGetValue(player.PlayerId, out string outputMessage))
                            outputMessage = string.Empty;

                        if (respawnManager._curSequence == RespawnManager.RespawnSequencePhase.PlayingEntryAnimations)
                        {
                            /*if (NoEndlessRoundHandler.SpawnSamsara)
                                InformRespawnSamsara(ttr, respawningMTF, notrespawningMTF, spawnQueue.ContainsKey(player.Id));
                            else */
                            if (respawnManager.NextKnownTeam == SpawnableTeamType.ChaosInsurgency)
                                respawnMsg = this.InformRespawnCI(ttr, respawningCI, notrespawningCI, this.spawnQueue.ContainsKey(player.PlayerId) ? this.spawnQueue[player.PlayerId].Role : RoleTypeId.None);
                            else if (respawnManager.NextKnownTeam == SpawnableTeamType.NineTailedFox)
                                respawnMsg = this.InformRespawnMTF(ttr, respawningMTF, notrespawningMTF, this.spawnQueue.ContainsKey(player.PlayerId) ? this.spawnQueue[player.PlayerId].Role : RoleTypeId.None, this.spawnQueue.FirstOrDefault(i => i.Value.Role == RoleTypeId.NtfCaptain).Value.Player?.DisplayNickname ?? "UNKNOWN");
                            else
                                respawnMsg = this.InformRespawnNone(ttr);
                        }
                        else
                            respawnMsg = respawnWaiting;

                        /*if (player.RemoteAdminAccess)
                        {
                            string adminMsg = "{masterAdminMessage}";
                            if (player.GetSessionVariable<bool>(SessionVarType.LONG_OVERWATCH))
                                adminMsg = "[<color=red>LONG OVERWATCH <b><color=yellow>ACTIVE</color></b></color>]";
                            else if (player.IsOverwatchEnabled && player.TryGetSessionVariable(SessionVarType.OVERWATCH_START_TIME, out DateTime checkTime))
                            {
                                var diff = checkTime.AddMinutes(5) - DateTime.Now;
                                adminMsg = $"[<color=yellow>OVERWATCH <b>ACTIVE</b> | {diff.Minutes:00}<color=yellow>:</color>{diff.Seconds:00}</color>]";
                            }
                            else if (player.IsOverwatchEnabled)
                                adminMsg = $"[<color=yellow>OVERWATCH <b>ACTIVE</b> | <color=yellow>UNKNOWN</color> overwatch time</color>]";

                            outputMessage += this.InformTTR(message, respawnMsg, true, adminMessage.Replace("{masterAdminMessage}", adminMsg));
                            outputMessage += "<br><br>" + this.InformSpectating(GetSpectatedPlayer(player), true);
                        }
                        else
                        {
                            outputMessage += this.InformTTR(message, respawnMsg, false, adminMessage);
                            outputMessage += "<br><br>" + this.InformSpectating(GetSpectatedPlayer(player), false);
                        }*/

                        outputMessage += this.InformTTR(message, respawnMsg, false, adminMessage);
                        outputMessage += "<br><br>" + this.InformSpectating(GetSpectatedPlayer(player), false);

                        // player.ShowHint(message, 2);
                        if (player.IsAlive)
                            player.SetGUI("specInfo", PseudoGUIPosition.MIDDLE, null);
                        else
                            player.SetGUI("specInfo", PseudoGUIPosition.MIDDLE, outputMessage);
                    }

                    // MasterHandler.LogTime("SpecInfoHandler", "TTRUpdate", start, DateTime.UtcNow);
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
        if (newRole != RoleTypeId.Spectator)
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

        Timing.CallDelayed(20, () => _respawnQueueSeed = -1);
    }

    [PluginEvent(ServerEventType.RoundRestart)]
    private void OnRoundRestart()
    {
        _respawnQueueSeed = -1;
        this._roundStarted = false;
    }

    [PluginEvent(ServerEventType.RoundStart)]
    private void OnRoundStart()
    {
        Timing.RunCoroutine(this.UpdateCache(), "UpdateCache");
        // Timing.CallDelayed(45, () => _is106 = Player.GetPlayers().Any(p => p.Role == RoleTypeId.Scp106));

        this._roundStarted = true;
    }

    [PluginEvent(ServerEventType.WaitingForPlayers)]
    private void OnWaitingForPlayers()
    {
        /*if (Exiled.Loader.Loader.Plugins.Any(x => x.Name == "BetterSCP-SCP049" && x.Config.IsEnabled))
            BetterSCP049Integration.Enabled = true;*/
    }

    private IEnumerator<float> UpdateCache()
    {
        yield return Timing.WaitForSeconds(1);
        /*int rid = RoundPlus.RoundId;
        _ = Warhead.Controller;
        _ = Warhead.OutsitePanel;
        _ = Warhead.SitePanel;
        _ = MapPlus.IsSCP079Recontained;*/
        while (Round.IsRoundStarted)
        {
            yield return Timing.WaitForSeconds(5);
            _counterCI = RespawnTokensManager.Counters.First(x => x.Team == SpawnableTeamType.ChaosInsurgency);
            _counterNTF = RespawnTokensManager.Counters.First(x => x.Team == SpawnableTeamType.NineTailedFox);

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
        /*else if (API.Utilities.Map.RespawnLock)
            masterAdminMessage = "[<color=yellow>RESPAWN LOCK <b>ACTIVE</b></color>]";
        else if (API.BetterWarheadHandler.Warhead.StopLock)
            masterAdminMessage = "[<color=yellow>WARHEAD STOP LOCK <b>ACTIVE</b></color>]";
        else if (API.BetterWarheadHandler.Warhead.StartLock)
            masterAdminMessage = "[<color=yellow>WARHEAD START LOCK <b>ACTIVE</b></color>]";
        else if (API.Utilities.Map.Overheat.OverheatLevel != -1)
            masterAdminMessage = $"[<color=red>OVERHEAT <b>ACTIVE</b>, STATE: {API.Utilities.Map.Overheat.OverheatLevel}</color>]";*/
        /*else if (RealPlayers.List.Count() < 4)
            masterAdminMessage = "[<color=yellow>LESS THAN 4 PLAYERS | <b>NOT SAVING</b> ACTIVITY</color>]";*/

        return preparedText.Replace("{respawnMsg}", respawnMsg) + (admin ? $"<br>{adminMessage.Replace("{masterAdminMessage}", masterAdminMessage)}" : string.Empty);
    }

    private string PrepareInfoText(int spectators, out string adminMessage)
    {
        var roundTimeString = string.Format(Plugin.Instance.Translation.RoundInfo, Round.Duration.Minutes.ToString("00"), Round.Duration.Seconds.ToString("00"));
        var specatorString = spectators < 2 ? Plugin.Instance.Translation.OnlySpectatorInfo : string.Format(Plugin.Instance.Translation.SpectatorInfo, spectators - 1);
        var playersString = string.Format(Plugin.Instance.Translation.PlayersInfo, ReferenceHub.HubByPlayerIds.Count, CustomNetworkManager.slots);
        /*var generatorString = string.Format(Plugin.Instance.Translation.GeneratorInfo, Scp079Recontainer.AllGenerators.Where(x => x.Engaged).Count().ToString()) + (_cache_nearestGenerator == null ? string.Empty : $" (<color=yellow>{Math.Round((double)(_cache_nearestGenerator?.Network_syncTime ?? -1))}</color>s)");
        var overchargeString = string.Format(Plugin.Instance.Translation.OverchargeInfo, MapPlus.IsSCP079Recontained ? "<color=yellow>Recontained</color>" : "<color=yellow>Recontainment ready</color>");
        var genString = MapPlus.IsSCP079ReadyForRecontainment || MapPlus.IsSCP079Recontained ? overchargeString : generatorString;
        if (Warhead.IsDetonated)
            genString = generatorString;
        var adminWarheadString = string.Format(
            PluginHandler.Instance.Translation.AdminWarheadInfo,
            BetterWarheadHandler.Warhead.LastStartUser?.Id.ToString() ?? "?",
            BetterWarheadHandler.Warhead.LastStartUser?.Nickname ?? "UNKNOWN",
            BetterWarheadHandler.Warhead.LastStopUser?.Id.ToString() ?? "?",
            BetterWarheadHandler.Warhead.LastStopUser?.Nickname ?? "UNKNOWN");*/
        adminMessage = string.Format(Plugin.Instance.Translation.AdminInfo, "{masterAdminMessage}", Mathf.RoundToInt(_counterNTF.Amount), Mathf.RoundToInt(_counterCI.Amount)/*, adminWarheadString*/);
        return $@"<size=50%>{roundTimeString}   |   {playersString}</size><br>{specatorString}<br>{{respawnMsg}}<br><br><br><br><br><br><br><br><br><br><br><br>"; // {roundTimeString}   |   {playersString}   |   {genString}
    }

    private string InformSpectating(Player player, bool admin)
    {
        if (player is null || player.IsAlive || player.GameObject == null)
            return string.Empty;

        var roleName = $"<color=#{player.RoleColor}>{player.Role}</color>";

        /*if (CustomRole.TryGet(player, out var roles))
            roleName = string.Join(", ", roles.Select(x => x.Name));*/

        string tor = $"{player.DisplayNickname} is <color=yellow>playing</color> as {roleName}";
        if (!CustomClassDescriptors.TryGetValue(player.Role, out var handler))
        {
            var currentItem = player.CurrentItem;
            if (currentItem != null)
            {
                /*if (CustomItem.TryGet(currentItem, out var customItem))
                {
                    tor += $"<br> and is holding {customItem.Name}";
                    if (customItem is CustomWeapon customfirearm)
                    {
                        var firearm = currentItem as Firearm;
                        tor += $" <color=yellow>{firearm.Ammo}</color>/<color=yellow>{customfirearm.ClipSize}</color>";
                        if (firearm.Aiming)
                            tor += " (<color=yellow>AIMING</color>)";
                    }
                }*/
                tor += $"<br> and is holding {currentItem.ItemTypeId}";
                if (currentItem is Firearm firearm)
                    tor += $" (<color=yellow>{firearm.Status.Ammo}</color>/<color=yellow>{firearm.AmmoManagerModule.MaxAmmo}</color>), ammo: <color=yellow>{(player.AmmoBag.TryGetValue(firearm.AmmoType, out var ammo) ? ammo : 0)}</color>";
                else if (currentItem is MicroHIDItem microHid)
                    tor += $" with <color=yellow>{Math.Floor(microHid.RemainingEnergy * 100)}%</color>";
            }
        }
        else
            tor += handler(player);

        if (admin)
            tor += AdminDescriptor(player);

        return "<size=50%>" + tor + "</size>";
    }

    private string InformRespawnWaiting(float ttr)
    {
        return string.Format(Plugin.Instance.Translation.RespawnInfo, ((ttr - (ttr % 60)) / 60).ToString("00"), (ttr % 60).ToString("00"));
    }

    private string InformRespawnNone(float ttr)
    {
        return string.Format(Plugin.Instance.Translation.RespawnNone, (ttr % 60).ToString("00"));
    }

    private string InformRespawnMTF(float ttr, float respawningMTF, float notrespawningMTF, RoleTypeId expectedRole, string commander)
    {
        string roleString = expectedRole == RoleTypeId.None ? string.Format(Plugin.Instance.Translation.RespawnMTFWillNotRespawn, commander) : string.Format(Plugin.Instance.Translation.RespawnMTFWillRespawn);
        switch (expectedRole)
        {
            case RoleTypeId.NtfPrivate:
                roleString += string.Format(Plugin.Instance.Translation.RespawnMTFWillRespawnPrivate, commander);
                break;

            case RoleTypeId.NtfSergeant:
                roleString += string.Format(Plugin.Instance.Translation.RespawnMTFWillRespawnSergeant, commander);
                break;

            case RoleTypeId.NtfCaptain:
                roleString += string.Format(Plugin.Instance.Translation.RespawnMTFWillRespawnCaptain);
                break;

                /*default:
                    roleString += "UNKNOWN";
                    break;*/
        }

        return string.Format(Plugin.Instance.Translation.RespawnMTFRespawn, (ttr % 60).ToString("00"), respawningMTF, notrespawningMTF, roleString);
    }

    private string InformRespawnCI(float ttr, float respawningCI, float notrespawningCI, RoleTypeId expectedRole)
    {
        string roleString = expectedRole == RoleTypeId.None ? string.Format(Plugin.Instance.Translation.RespawnCIWillNotRespawn) : string.Format(Plugin.Instance.Translation.RespawnCIWillRespawn);
        switch (expectedRole)
        {
            case RoleTypeId.ChaosRifleman:
                roleString += string.Format(Plugin.Instance.Translation.RespawnCIWillRespawnRifleman);
                break;

            case RoleTypeId.ChaosRepressor:
                roleString += string.Format(Plugin.Instance.Translation.RespawnCIWillRespawnRepressor);
                break;

            case RoleTypeId.ChaosMarauder:
                roleString += string.Format(Plugin.Instance.Translation.RespawnCIWillRespawnMarauder);
                break;

                /*default:
                    roleString += "UNKNOWN";
                    break;*/
        }

        return string.Format(Plugin.Instance.Translation.RespawnCIRespawn, (ttr % 60).ToString("00"), respawningCI, notrespawningCI, roleString);
    }
}
