// -----------------------------------------------------------------------
// <copyright file="SpecInfoHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using MEC;
using Mistaken.API;
using Mistaken.API.Diagnostics;
using Mistaken.API.Extensions;
using Mistaken.API.GUI;
using UnityEngine;

namespace Mistaken.SpectatorGUI
{
    internal class SpecInfoHandler : Module
    {
        public static void AddDeathMessage(Player player, string message)
        {
            if (DeathMessages.ContainsKey(player.Id))
                DeathMessages.Remove(player.Id);
            DeathMessages.Add(player.Id, message);
            Module.CallSafeDelayed(15, () => DeathMessages.Remove(player.Id), "SpecInfo.AddDeathMessage");
        }

        public SpecInfoHandler(PluginHandler p)
            : base(p)
        {
        }

        public override string Name => "SpecInfo";

        public override void OnEnable()
        {
            Exiled.Events.Handlers.Server.RoundStarted += this.Handle(() => this.Server_RoundStarted(), "RoundStart");
            Exiled.Events.Handlers.Server.RestartingRound += this.Handle(() => this.Server_RestartingRound(), "RoundRestart");
            Exiled.Events.Handlers.Server.RespawningTeam += this.Handle<Exiled.Events.EventArgs.RespawningTeamEventArgs>((ev) => this.Server_RespawningTeam(ev));
            Exiled.Events.Handlers.Player.ChangingRole += this.Handle<Exiled.Events.EventArgs.ChangingRoleEventArgs>((ev) => this.Player_ChangingRole(ev));
        }

        public override void OnDisable()
        {
            Exiled.Events.Handlers.Server.RoundStarted -= this.Handle(() => this.Server_RoundStarted(), "RoundStart");
            Exiled.Events.Handlers.Server.RestartingRound -= this.Handle(() => this.Server_RestartingRound(), "RoundRestart");
            Exiled.Events.Handlers.Server.RespawningTeam -= this.Handle<Exiled.Events.EventArgs.RespawningTeamEventArgs>((ev) => this.Server_RespawningTeam(ev));
            Exiled.Events.Handlers.Player.ChangingRole -= this.Handle<Exiled.Events.EventArgs.ChangingRoleEventArgs>((ev) => this.Player_ChangingRole(ev));
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

        private void Player_ChangingRole(Exiled.Events.EventArgs.ChangingRoleEventArgs ev)
        {
            if (ev.NewRole != RoleType.Spectator)
                ev.Player.SetGUI("specInfo", PseudoGUIPosition.MIDDLE, null);
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
            this.CallDelayed(
                20,
                () =>
                {
                    this.respawnQueueSeed = -1;
                },
                "RespawningTeam");
        }

        private void Server_RestartingRound()
        {
            this.respawnQueueSeed = -1;
        }

        private void Server_RoundStarted()
        {
            cache_maxCI = Respawning.RespawnWaveGenerator.SpawnableTeams[Respawning.SpawnableTeamType.ChaosInsurgency].MaxWaveSize;
            cache_maxMTF = Respawning.RespawnWaveGenerator.SpawnableTeams[Respawning.SpawnableTeamType.NineTailedFox].MaxWaveSize;

            this.RunCoroutine(this.TTRUpdate(), "TTRUpdate");
            this.RunCoroutine(this.UpdateCache(), "UpdateCache");
            this.CallDelayed(
                45,
                () =>
                {
                    is106 = RealPlayers.List.Any(p => p.Role == RoleType.Scp106);
                },
                "Update106Info");
        }

        private IEnumerator<float> UpdateCache()
        {
            yield return Timing.WaitForSeconds(1);
            int rid = RoundPlus.RoundId;
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

        private IEnumerator<float> TTRUpdate()
        {
            yield return Timing.WaitForSeconds(1);
            int rid = RoundPlus.RoundId;
            while (Round.IsStarted && rid == RoundPlus.RoundId)
            {
                yield return Timing.WaitForSeconds(1);

                var spectators = RealPlayers.Get(RoleType.Spectator);
                var spectatorsCount = spectators.Count();

                if (spectatorsCount == 0)
                    continue;
                try
                {
                    var start = DateTime.Now;
                    var respawnManager = Respawning.RespawnManager.Singleton;

                    var toRespawn = RealPlayers.List.Where(p => p.IsDead && !p.IsOverwatchEnabled).Count();

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
                            List<Player> list = RealPlayers.List.Where(p => p.IsDead && !p.IsOverwatchEnabled).OrderBy(rh => rh.ReferenceHub.characterClassManager.DeathTime).ToList();
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
                        if (player.CheckPermissions(PlayerPermissions.AdminChat))
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
                            outputMessage += "<br><br><br><br>" + this.InformSpectating(Player.Get(player.ReferenceHub.spectatorManager.CurrentSpectatedPlayer), true);
                        }
                        else
                        {
                            outputMessage += this.InformTTR(message, player, false, adminMessage);
                            outputMessage += "<br><br><br><br>" + this.InformSpectating(Player.Get(player.ReferenceHub.spectatorManager.CurrentSpectatedPlayer), false);
                        }

                        // player.ShowHint(message, 2);
                        player.SetGUI("specInfo", PseudoGUIPosition.MIDDLE, "<br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br>" + outputMessage);
                    }

                    MasterHandler.LogTime("SpecInfoHandler", "TTRUpdate", start, DateTime.Now);
                }
                catch (System.Exception ex)
                {
                    this.Log.Error(ex.Message);
                    this.Log.Error(ex.StackTrace);
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
            var overchargeString = string.Format(PluginHandler.Instance.Translation.OverchargeInfo, Events.Handlers.CustomEvents.SCP079.IsRecontained ? "<color=yellow>Recontained</color>" : "<color=yellow>Recontainment ready</color>");
            var genString = Events.Handlers.CustomEvents.SCP079.IsBeingRecontained | Events.Handlers.CustomEvents.SCP079.IsRecontained ? overchargeString : generatorString;
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

            string tor = $"{player.GetDisplayName()} is <color=yellow>playing</color> as <color={player.RoleColor.ToHex()}>{player.Role}</color>";

            if (player.CurrentItem != null)
                tor += $"<br> and is holding {player.CurrentItem.Type}{(player.CurrentItem is Firearm firearm ? $" <color=yellow>{firearm.Ammo}</color>/<color=yellow>{firearm.MaxAmmo}</color> of <color=yellow>{firearm.AmmoType}</color> {(firearm.Aiming ? " (<color=yellow>AIMING</color>)" : string.Empty)}" : string.Empty)}";

            if (admin)
            {
                tor += $"<br>Id: {player.Id}";
            }

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

                default:
                    roleString += "UNKNOWN";
                    break;
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

                default:
                    roleString += "UNKNOWN";
                    break;
            }

            return string.Format(PluginHandler.Instance.Translation.RespawnCIRespawn, (ttr % 60).ToString("00"), respawningCI, notrespawningCI, roleString);
        }
    }
}
