﻿// -----------------------------------------------------------------------
// <copyright file="SpecInfoHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.CustomItems.API.Features;
using Exiled.CustomRoles.API.Features;
using MEC;
using Mistaken.API;
using Mistaken.API.Diagnostics;
using Mistaken.API.Extensions;
using Mistaken.API.GUI;
using UnityEngine;

namespace Mistaken.SpectatorGUI
{
    /// <inheritdoc/>
    public class SpecInfoHandler : Module
    {
        /// <summary>
         /// Dictionary containing Custom class descryptors to describe classes.
         /// </summary>
        public static readonly Dictionary<RoleType, Func<Player, string>> CustomClassDescriptors = new Dictionary<RoleType, Func<Player, string>>();

        static SpecInfoHandler()
        {
            CustomClassDescriptors[RoleType.Scp096] = (player) =>
            {
                var scp = player.CurrentScp as PlayableScps.Scp096;
                var rageText = $"<br> Rage left: <color=yellow>{Mathf.RoundToInt(scp.EnrageTimeLeft)}</color>s<br>Targets: <color=yellow>{Mathf.RoundToInt(scp._targets.Count)}</color>";
                var cooldownText = $"<br> Cooldown left: <color=yellow>{Mathf.RoundToInt(scp.RemainingEnrageCooldown)}</color>s";

                switch (scp.PlayerState)
                {
                    case PlayableScps.Scp096PlayerState.Enraging:
                    case PlayableScps.Scp096PlayerState.Enraged:
                    case PlayableScps.Scp096PlayerState.Attacking:
                    case PlayableScps.Scp096PlayerState.PryGate:
                    case PlayableScps.Scp096PlayerState.Charging:
                        return rageText;

                    case PlayableScps.Scp096PlayerState.TryNotToCry:
                    case PlayableScps.Scp096PlayerState.Docile:
                    case PlayableScps.Scp096PlayerState.Calming:
                        return scp.RemainingEnrageCooldown != 0 ? cooldownText : string.Empty;

                    default:
                        return string.Empty;
                }
            };
        }

        /// <summary>
        /// Gets or sets adds info about spectated player if spectating player is admin.
        /// </summary>
        public static Func<Player, string> AdminDescriptor { get; set; } = (player) =>
        {
            return $"<br>Id: {player.Id}";
        };

        /// <inheritdoc/>
        public override string Name => "SpecInfo";

        /// <inheritdoc/>
        public override void OnEnable()
        {
            Exiled.Events.Handlers.Server.RoundStarted += this.Server_RoundStarted;
            Exiled.Events.Handlers.Server.RestartingRound += this.Server_RestartingRound;
            Exiled.Events.Handlers.Server.RespawningTeam += this.Server_RespawningTeam;
            Exiled.Events.Handlers.Player.ChangingRole += this.Player_ChangingRole;

            // Events.Handlers.CustomEvents.ChangingSpectatedPlayer += this.Handle<Events.EventArgs.ChangingSpectatedPlayerEventArgs>((ev) => this.CustomEvents_ChangingSpectatedPlayer(ev));
            this.active = true;
            Task.Run(async () =>
            {
                while (this.active)
                {
                    await Task.Delay(1000);

                    if (!this.roundStarted)
                        continue;

                    var spectators = RealPlayers.Get(RoleType.Spectator);
                    var spectatorsCount = spectators.Count();

                    if (spectatorsCount == 0)
                        continue;
                    try
                    {
                        var start = DateTime.UtcNow;
                        var respawnManager = Respawning.RespawnManager.Singleton;

                        var toRespawnList = RealPlayers.List.Where(p => p.IsDead && !p.IsOverwatchEnabled);
                        var toRespawn = toRespawnList.Count();

                        var respawningCI = Math.Min(Dynamic_maxRespawnCI, toRespawn);
                        var notrespawningCI = toRespawn - respawningCI;

                        var respawningMTF = Math.Min(Dynamic_maxRespawnMTF, toRespawn);
                        var notrespawningMTF = toRespawn - respawningMTF;

                        var ttr = Mathf.RoundToInt(respawnManager._timeForNextSequence - (float)respawnManager._stopwatch.Elapsed.TotalSeconds);

                        this.spawnQueue.Clear();
                        if (respawnManager._curSequence == Respawning.RespawnManager.RespawnSequencePhase.PlayingEntryAnimations)
                        {
                            if (Respawning.RespawnWaveGenerator.SpawnableTeams.TryGetValue(respawnManager.NextKnownTeam, out Respawning.SpawnableTeamHandlerBase spawnableTeam))
                            {
                                List<Player> list = toRespawnList.OrderBy(rh => rh.ReferenceHub.characterClassManager.DeathTime).ToList();
                                int maxRespawnablePlayers = respawnManager.NextKnownTeam == Respawning.SpawnableTeamType.ChaosInsurgency ? Dynamic_maxRespawnCI : Dynamic_maxRespawnMTF;
                                maxRespawnablePlayers = Math.Max(maxRespawnablePlayers, 0);

                                while (list.Count > maxRespawnablePlayers)
                                    list.RemoveAt(list.Count - 1);

                                // foreach (var player in list)
                                //     this.spawnQueue.Add(player.Id, (player, RoleType.Spectator));
                                if (this.respawnQueueSeed == -1)
                                    this.respawnQueueSeed = UnityEngine.Random.Range(0, 10000);
                                list.Shuffle(this.respawnQueueSeed);
                                Queue<RoleType> queue = new Queue<RoleType>();
                                spawnableTeam.GenerateQueue(queue, list.Count);
                                foreach (var player in list)
                                {
                                    try
                                    {
                                        this.spawnQueue.Add(player.Id, (player, queue.Dequeue()));
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }

                                NorthwoodLib.Pools.ListPool<Player>.Shared.Return(list);
                            }
                        }

                        string message = this.PrepareInfoText(spectatorsCount, out string adminMessage);

                        foreach (var player in spectators)
                        {
                            if (!DeathMessages.TryGetValue(player.Id, out string outputMessage))
                                outputMessage = string.Empty;
                            if (respawnManager._curSequence == Respawning.RespawnManager.RespawnSequencePhase.PlayingEntryAnimations)
                            {
                                /*if (NoEndlessRoundHandler.SpawnSamsara)
                                    InformRespawnSamsara(ttr, respawningMTF, notrespawningMTF, spawnQueue.ContainsKey(player.Id));
                                else */
                                if (respawnManager.NextKnownTeam == Respawning.SpawnableTeamType.ChaosInsurgency)
                                    outputMessage += this.InformRespawnCI(ttr, respawningCI, notrespawningCI, this.spawnQueue.ContainsKey(player.Id) ? this.spawnQueue[player.Id].Role : RoleType.None);
                                else if (respawnManager.NextKnownTeam == Respawning.SpawnableTeamType.NineTailedFox)
                                    outputMessage += this.InformRespawnMTF(ttr, respawningMTF, notrespawningMTF, this.spawnQueue.ContainsKey(player.Id) ? this.spawnQueue[player.Id].Role : RoleType.None, this.spawnQueue.FirstOrDefault(i => i.Value.Role == RoleType.NtfCaptain).Value.Player?.GetDisplayName() ?? "UNKNOWN");
                                else
                                    outputMessage += this.InformRespawnNone(ttr);
                            }
                            else
                                outputMessage += this.InformRespawnWaiting(ttr);
                            if (player.RemoteAdminAccess)
                            {
                                string adminMsg = "{masterAdminMessage}";
                                if (player.GetSessionVar<bool>(SessionVarType.LONG_OVERWATCH))
                                    adminMsg = "[<color=red>LONG OVERWATCH <b><color=yellow>ACTIVE</color></b></color>]";
                                else if (player.IsOverwatchEnabled && player.TryGetSessionVariable<DateTime>("OVERWATCH_START", out DateTime checkTime))
                                {
                                    var diff = checkTime.AddMinutes(5) - DateTime.Now;
                                    adminMsg = $"[<color=yellow>OVERWATCH <b>ACTIVE</b> | {diff.Minutes:00}<color=yellow>:</color>{diff.Seconds:00}</color>]";
                                }
                                else if (player.IsOverwatchEnabled)
                                    adminMsg = $"[<color=yellow>OVERWATCH <b>ACTIVE</b> | <color=yellow>UNKNOWN</color> overwatch time</color>]";

                                outputMessage += this.InformTTR(message, player, true, adminMessage.Replace("{masterAdminMessage}", adminMsg));
                                player.SetGUI("specInfo_observing", PseudoGUIPosition.BOTTOM, this.InformSpectating(Player.Get(player.ReferenceHub.spectatorManager.CurrentSpectatedPlayer), true));
                            }
                            else
                            {
                                outputMessage += this.InformTTR(message, player, false, adminMessage);
                                player.SetGUI("specInfo_observing", PseudoGUIPosition.BOTTOM, this.InformSpectating(Player.Get(player.ReferenceHub.spectatorManager.CurrentSpectatedPlayer), false));
                            }

                            // player.ShowHint(message, 2);
                            player.SetGUI("specInfo", PseudoGUIPosition.MIDDLE, "<br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br>" + outputMessage);
                        }

                        MasterHandler.LogTime("SpecInfoHandler", "TTRUpdate", start, DateTime.UtcNow);
                    }
                    catch (System.Exception ex)
                    {
                        this.Log.Error(ex.Message);
                        this.Log.Error(ex.StackTrace);
                    }
                }
            });
        }

        /// <inheritdoc/>
        public override void OnDisable()
        {
            Exiled.Events.Handlers.Server.RoundStarted -= this.Server_RoundStarted;
            Exiled.Events.Handlers.Server.RestartingRound -= this.Server_RestartingRound;
            Exiled.Events.Handlers.Server.RespawningTeam -= this.Server_RespawningTeam;
            Exiled.Events.Handlers.Player.ChangingRole -= this.Player_ChangingRole;

            // Events.Handlers.CustomEvents.ChangingSpectatedPlayer -= this.Handle<Events.EventArgs.ChangingSpectatedPlayerEventArgs>((ev) => this.CustomEvents_ChangingSpectatedPlayer(ev));
            this.active = false;
        }

        internal SpecInfoHandler(PluginHandler p)
            : base(p)
        {
        }

        private static readonly Dictionary<int, string> DeathMessages = new Dictionary<int, string>();
        private static bool is106 = false;
        private static int cache_ticketsCI;
        private static int cache_ticketsMTF;
        private static int cache_maxCI;
        private static int cache_maxMTF;
        private static MapGeneration.Distributors.Scp079Generator cache_nearestGenerator;

        private static int Dynamic_maxRespawnCI => Math.Min(cache_maxCI, cache_ticketsCI == 0 ? 5 : cache_ticketsCI);

        private static int Dynamic_maxRespawnMTF => Math.Min(cache_maxMTF, cache_ticketsMTF);

        private readonly Dictionary<int, (Player Player, RoleType Role)> spawnQueue = new Dictionary<int, (Player Player, RoleType Role)>();
        private int respawnQueueSeed = -1;

        private bool active;
        private bool roundStarted;

        /*private void CustomEvents_ChangingSpectatedPlayer(Events.EventArgs.ChangingSpectatedPlayerEventArgs ev)
        {
            if (ev.NewPlayer != ev.Spectator)
                ev.NewPlayer.SetGUI("specInfo_observing", PseudoGUIPosition.BOTTOM, this.InformSpectating(ev.NewPlayer, ev.Spectator.RemoteAdminAccess));
        }*/

        private void Player_ChangingRole(Exiled.Events.EventArgs.ChangingRoleEventArgs ev)
        {
            if (ev.NewRole != RoleType.Spectator)
            {
                ev.Player.SetGUI("specInfo", PseudoGUIPosition.MIDDLE, null);
                ev.Player.SetGUI("specInfo_observing", PseudoGUIPosition.BOTTOM, null);
            }
        }

        private void Server_RespawningTeam(Exiled.Events.EventArgs.RespawningTeamEventArgs ev)
        {
            if (Respawning.RespawnWaveGenerator.SpawnableTeams.TryGetValue(ev.NextKnownTeam, out var spawnableTeam) || ev.NextKnownTeam == Respawning.SpawnableTeamType.None)
            {
                int tickets = Respawning.RespawnTickets.Singleton.GetAvailableTickets(ev.NextKnownTeam);
                if (tickets == 0)
                {
                    tickets = 5;
                    Respawning.RespawnTickets.Singleton.GrantTickets(Respawning.SpawnableTeamType.ChaosInsurgency, 5, true);
                }

                ev.MaximumRespawnAmount = Mathf.Min(tickets, spawnableTeam.MaxWaveSize);
            }

            ev.MaximumRespawnAmount = Mathf.Max(ev.MaximumRespawnAmount, 0);
            while (ev.Players.Count > ev.MaximumRespawnAmount)
                ev.Players.RemoveAt(ev.Players.Count - 1);
            ev.Players.Shuffle(this.respawnQueueSeed);

            // ev.Players.Clear();
            // foreach (var item in SpawnQueue)
            //    ev.Players.Add(item);
            this.CallDelayed(20, () => this.respawnQueueSeed = -1, "RespawningTeam");
        }

        private void Server_RestartingRound()
        {
            this.respawnQueueSeed = -1;
            this.roundStarted = false;
        }

        private void Server_RoundStarted()
        {
            cache_maxCI = Respawning.RespawnWaveGenerator.SpawnableTeams[Respawning.SpawnableTeamType.ChaosInsurgency].MaxWaveSize;
            cache_maxMTF = Respawning.RespawnWaveGenerator.SpawnableTeams[Respawning.SpawnableTeamType.NineTailedFox].MaxWaveSize;

            this.RunCoroutine(this.UpdateCache(), "UpdateCache");
            this.CallDelayed(
                45,
                () =>
                {
                    is106 = RealPlayers.List.Any(p => p.Role == RoleType.Scp106);
                },
                "Update106Info");

            this.roundStarted = true;
        }

        private IEnumerator<float> UpdateCache()
        {
            yield return Timing.WaitForSeconds(1);
            int rid = RoundPlus.RoundId;
            _ = Warhead.Controller;
            _ = Warhead.OutsitePanel;
            _ = Warhead.SitePanel;
            _ = MapPlus.LureSubjectContainer;
            _ = MapPlus.IsSCP079Recontained;
            while (Round.IsStarted && rid == RoundPlus.RoundId)
            {
                yield return Timing.WaitForSeconds(5);
                cache_ticketsCI = Respawning.RespawnTickets.Singleton.GetAvailableTickets(Respawning.SpawnableTeamType.ChaosInsurgency);
                cache_ticketsMTF = Respawning.RespawnTickets.Singleton.GetAvailableTickets(Respawning.SpawnableTeamType.NineTailedFox);

                cache_nearestGenerator = null;
                foreach (var generator in Recontainer079.AllGenerators)
                {
                    if (generator.Activating)
                    {
                        if ((cache_nearestGenerator?.Network_syncTime ?? float.MaxValue) > generator.Network_syncTime)
                            cache_nearestGenerator = generator;
                    }
                }
            }
        }

        private string InformTTR(string preparedText, Player player, bool admin, string adminMessage)
        {
            string masterAdminMessage = string.Empty;
            if (Round.IsLocked)
                masterAdminMessage = "[<color=yellow>ROUND LOCK <b>ACTIVE</b></color>]";
            else if (API.Utilities.Map.RespawnLock)
                masterAdminMessage = "[<color=yellow>RESPAWN LOCK <b>ACTIVE</b></color>]";
            /*else if (RealPlayers.List.Count() < 4)
                masterAdminMessage = "[<color=yellow>LESS THAN 4 PLAYERS | <b>NOT SAVING</b> ACTIVITY</color>]";*/

            var deadTime = DateTime.Now - new DateTime(player.ReferenceHub.characterClassManager.DeathTime);
            var deadTimeString = string.Format(PluginHandler.Instance.Translation.DeadTimeInfo, deadTime.Minutes.ToString("00"), deadTime.Seconds.ToString("00"));
            return preparedText.Replace("{DeadTime}", deadTimeString) + (admin ? $"<br>{adminMessage.Replace("{masterAdminMessage}", masterAdminMessage)}" : string.Empty);
        }

        private string PrepareInfoText(int spectators, out string adminMessage)
        {
            var systemTimeString = string.Format(PluginHandler.Instance.Translation.TimeInfo, DateTime.Now.ToString("HH:mm:ss").Replace(":", "</color>:<color=yellow>"));
            var lczTime = (float)LightContainmentZoneDecontamination.DecontaminationController.Singleton.DecontaminationPhases.First(d => d.Function == LightContainmentZoneDecontamination.DecontaminationController.DecontaminationPhase.PhaseFunction.Final).TimeTrigger - (float)LightContainmentZoneDecontamination.DecontaminationController.GetServerTime;
            var lczString = string.Format(PluginHandler.Instance.Translation.LCZInfo, ((lczTime - (lczTime % 60)) / 60).ToString("00"), Mathf.RoundToInt(lczTime % 60).ToString("00"));
            if (lczTime < 0)
                lczString = PluginHandler.Instance.Translation.LCZInfoDecontcaminated;
            if (Warhead.IsInProgress)
                lczString = string.Format(PluginHandler.Instance.Translation.WarheadInfo, Warhead.DetonationTimer.ToString("00"));
            if (Warhead.IsDetonated)
                lczString = PluginHandler.Instance.Translation.WarheadInfoDetonated;
            var roundTimeString = string.Format(PluginHandler.Instance.Translation.RoundInfo, Round.ElapsedTime.Minutes.ToString("00"), Round.ElapsedTime.Seconds.ToString("00"));
            var specatorString = spectators < 2 ? PluginHandler.Instance.Translation.OnlySpectatorInfo : string.Format(PluginHandler.Instance.Translation.SpectatorInfo, spectators - 1);
            var playersString = string.Format(PluginHandler.Instance.Translation.PlayersInfo, PlayerManager.players.Count, CustomNetworkManager.slots);
            var generatorString = string.Format(PluginHandler.Instance.Translation.GeneratorInfo, Map.ActivatedGenerators.ToString()) + (cache_nearestGenerator == null ? string.Empty : $" (<color=yellow>{Math.Round((double)(cache_nearestGenerator?.Network_syncTime ?? -1))}</color>s)");
            var overchargeString = string.Format(PluginHandler.Instance.Translation.OverchargeInfo, MapPlus.IsSCP079Recontained ? "<color=yellow>Recontained</color>" : "<color=yellow>Recontainment ready</color>");
            var genString = MapPlus.IsSCP079ReadyForRecontainment || MapPlus.IsSCP079Recontained ? overchargeString : generatorString;
            if (Warhead.IsDetonated)
                genString = generatorString;
            var recontainmentReadyString = PluginHandler.Instance.Translation.RecontainmentReady;
            var recontainmentNotReadyString = PluginHandler.Instance.Translation.RecontainmentNotReady;
            var recontainmentContainedyString = PluginHandler.Instance.Translation.RecontainmentContained;
            var recontainmentString = MapPlus.FemurBreaked ? recontainmentContainedyString : (MapPlus.Lured ? recontainmentReadyString : recontainmentNotReadyString);
            var miscString = is106 ? recontainmentString : "[<color=yellow>REDACTED</color>]";
            var adminWarheadString = string.Format(
                PluginHandler.Instance.Translation.AdminWarheadInfo,
                Warhead.LeverStatus ? (Warhead.CanBeStarted ? "<color=green>Ready</color>" : "<color=blue>Cooldown</color>") : "<color=red>Disabled</color>",
                Warhead.IsKeycardActivated,
                BetterWarheadHandler.Warhead.LastStartUser?.Id.ToString() ?? "?",
                BetterWarheadHandler.Warhead.LastStartUser?.Nickname ?? "UNKNOWN",
                BetterWarheadHandler.Warhead.LastStopUser?.Id.ToString() ?? "?",
                BetterWarheadHandler.Warhead.LastStopUser?.Nickname ?? "UNKNOWN",
                BetterWarheadHandler.Warhead.StartLock,
                BetterWarheadHandler.Warhead.StopLock);
            adminMessage = string.Format(PluginHandler.Instance.Translation.AdminInfo, "{masterAdminMessage}", cache_ticketsMTF, cache_ticketsCI, adminWarheadString);
            return $"<br><br><br>{specatorString}<br><size=50%>{roundTimeString}   |   {{DeadTime}}   |   {playersString}<br>{lczString}   |   {systemTimeString}<br>{genString}   |   {miscString}</size>";
        }

        private string InformSpectating(Player player, bool admin)
        {
            if (player?.IsDead ?? true || (!player?.IsConnected ?? true))
                return string.Empty;

            var roleName = $"<color={player.RoleColor.ToHex()}>{player.Role}</color>";

            if (CustomRole.TryGet(player, out var roles))
            {
                if (roles.Count > 0)
                    roleName = string.Join(", ", roles.Select(x => x.Name));
            }

            string tor = $"{player.GetDisplayName()} is <color=yellow>playing</color> as {roleName}";
            if (!CustomClassDescriptors.TryGetValue(player.Role, out var handler))
            {
                var currentItem = player.CurrentItem;
                if (currentItem != null)
                {
                    if (CustomItem.TryGet(currentItem, out var customItem))
                    {
                        tor += $"<br> and is holding {customItem.Name}";
                        if (customItem is CustomWeapon customfirearm)
                        {
                            var firearm = currentItem as Firearm;
                            tor += $" <color=yellow>{firearm.Ammo}</color>/<color=yellow>{customfirearm.ClipSize}</color>";
                            if (firearm.Aiming)
                                tor += " (<color=yellow>AIMING</color>)";
                        }
                    }
                    else
                    {
                        tor += $"<br> and is holding {currentItem.Type}";
                        if (currentItem is Firearm firearm)
                        {
                            tor += $" <color=yellow>{firearm.Ammo}</color>/<color=yellow>{firearm.MaxAmmo}</color> of <color=yellow>{firearm.AmmoType}</color>";
                            if (firearm.Aiming)
                                tor += " (<color=yellow>AIMING</color>)";

                            var ammoType = firearm.AmmoType.GetItemType();
                            tor += $"<br>{firearm.AmmoType}: <color=yellow>{(player.Ammo.TryGetValue(ammoType, out var ammo) ? ammo : 0)}</color>";
                        }
                        else if (currentItem is MicroHid microHid)
                        {
                            tor += $" with <color=yellow>{Math.Floor(microHid.Energy * 100)}%</color>";
                        }
                    }
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
            return string.Format(PluginHandler.Instance.Translation.RespawnInfo, ((ttr - (ttr % 60)) / 60).ToString("00"), (ttr % 60).ToString("00"));
        }

        private string InformRespawnNone(float ttr)
        {
            return string.Format(PluginHandler.Instance.Translation.RespawnNone, (ttr % 60).ToString("00"));
        }

        private string InformRespawnMTF(float ttr, int respawningMTF, int notrespawningMTF, RoleType expectedRole, string commander)
        {
            string roleString = expectedRole == RoleType.None ? string.Format(PluginHandler.Instance.Translation.RespawnMTFWillNotRespawn, commander) : string.Format(PluginHandler.Instance.Translation.RespawnMTFWillRespawn);
            switch (expectedRole)
            {
                case RoleType.NtfPrivate:
                    roleString += string.Format(PluginHandler.Instance.Translation.RespawnMTFWillRespawnPrivate, commander);
                    break;
                case RoleType.NtfSergeant:
                    roleString += string.Format(PluginHandler.Instance.Translation.RespawnMTFWillRespawnSergeant, commander);
                    break;
                case RoleType.NtfCaptain:
                    roleString += string.Format(PluginHandler.Instance.Translation.RespawnMTFWillRespawnCaptain);
                    break;

                /*default:
                    roleString += "UNKNOWN";
                    break;*/
            }

            return string.Format(PluginHandler.Instance.Translation.RespawnMTFRespawn, (ttr % 60).ToString("00"), respawningMTF, notrespawningMTF, roleString);
        }

        private string InformRespawnCI(float ttr, int respawningCI, int notrespawningCI, RoleType expectedRole)
        {
            string roleString = expectedRole == RoleType.None ? string.Format(PluginHandler.Instance.Translation.RespawnCIWillNotRespawn) : string.Format(PluginHandler.Instance.Translation.RespawnCIWillRespawn);
            switch (expectedRole)
            {
                case RoleType.ChaosRifleman:
                    roleString += string.Format(PluginHandler.Instance.Translation.RespawnCIWillRespawnRifleman);
                    break;
                case RoleType.ChaosRepressor:
                    roleString += string.Format(PluginHandler.Instance.Translation.RespawnCIWillRespawnRepressor);
                    break;
                case RoleType.ChaosMarauder:
                    roleString += string.Format(PluginHandler.Instance.Translation.RespawnCIWillRespawnMarauder);
                    break;

                /*default:
                    roleString += "UNKNOWN";
                    break;*/
            }

            return string.Format(PluginHandler.Instance.Translation.RespawnCIRespawn, (ttr % 60).ToString("00"), respawningCI, notrespawningCI, roleString);
        }
    }
}
