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

        private void Player_ChangingRole(Exiled.Events.EventArgs.ChangingRoleEventArgs ev)
        {
            if (ev.NewRole != RoleType.Spectator)
                ev.Player.SetGUI("specInfo", PseudoGUIPosition.MIDDLE, null);
        }

        //private int RespawnQueueSeed = -1;
        private void Server_RespawningTeam(Exiled.Events.EventArgs.RespawningTeamEventArgs ev)
        {
            /*ev.ShuffleList = false;

            if (Respawning.RespawnWaveGenerator.SpawnableTeams.TryGetValue(ev.NextKnownTeam, out Respawning.SpawnableTeam spawnableTeam) || ev.NextKnownTeam == Respawning.SpawnableTeamType.None)
            {
                int num = Respawning.RespawnTickets.Singleton.GetAvailableTickets(ev.NextKnownTeam);
                if (num == 0)
                {
                    num = 5;
                    Respawning.RespawnTickets.Singleton.GrantTickets(Respawning.SpawnableTeamType.ChaosInsurgency, 5, true);
                }
                ev.MaximumRespawnAmount = Mathf.Min(num, spawnableTeam.MaxWaveSize);
            }
            ev.MaximumRespawnAmount = Mathf.Max(ev.MaximumRespawnAmount, 0);
            while (ev.Players.Count > ev.MaximumRespawnAmount)
                ev.Players.RemoveAt(ev.Players.Count - 1);
            ev.Players.Shuffle(RespawnQueueSeed);
            //ev.Players.Clear();
            //foreach (var item in SpawnQueue)
            //    ev.Players.Add(item);
            this.CallDelayed(20, () =>
            {
                RespawnQueueSeed = -1;
            }, "RespawningTeam");*/
        }

        private void Server_RestartingRound()
        {
            //RespawnQueueSeed = -1;
        }

        private void Server_RoundStarted()
        {
            cache_maxCI = Respawning.RespawnWaveGenerator.SpawnableTeams[Respawning.SpawnableTeamType.ChaosInsurgency].MaxWaveSize;
            cache_maxMTF = Respawning.RespawnWaveGenerator.SpawnableTeams[Respawning.SpawnableTeamType.NineTailedFox].MaxWaveSize;

            this.RunCoroutine(this.TTRUpdate(), "TTRUpdate");
            this.RunCoroutine(this.UpdateCache(), "UpdateCache");
            this.CallDelayed(45, () =>
            {
                Is106 = RealPlayers.List.Any(p => p.Role == RoleType.Scp106);
            }, "Update106Info");
        }

        private static bool Is106 = false;
        private readonly Dictionary<int, (Player Player, RoleType Role)> spawnQueue = new Dictionary<int, (Player Player, RoleType Role)>();
        private static readonly Dictionary<int, string> DeathMessages = new Dictionary<int, string>();

        public static void AddDeathMessage(Player player, string message)
        {
            if (DeathMessages.ContainsKey(player.Id))
                DeathMessages.Remove(player.Id);
            DeathMessages.Add(player.Id, message);
            Module.CallSafeDelayed(15, () => DeathMessages.Remove(player.Id), "SpecInfo.AddDeathMessage");
        }

        public static int cache_ticketsCI;
        public static int cache_ticketsMTF;
        public static int cache_maxCI;
        public static int cache_maxMTF;

        public static MapGeneration.Distributors.Scp079Generator cache_nearestGenerator;

        public static int Dynamic_maxRespawnCI => Math.Min(cache_maxCI, cache_ticketsCI == 0 ? 5 : cache_ticketsCI);

        public static int Dynamic_maxRespawnMTF => Math.Min(cache_maxMTF, cache_ticketsMTF);

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
                if (!RealPlayers.Any(RoleType.Spectator))
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

                    var spectators = RealPlayers.Get(RoleType.Spectator).Count();

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

                            foreach (var player in list)
                                this.spawnQueue.Add(player.Id, (player, RoleType.Spectator));

                            /*if (RespawnQueueSeed == -1)
                                RespawnQueueSeed = UnityEngine.Random.Range(0, 10000);
                            list.Shuffle(RespawnQueueSeed);
                            RoleType classid;
                            foreach (var player in list)
                            {
                                try
                                {
                                    classid = spawnableTeam.ClassQueue[Mathf.Min(spawnQueue.Count, spawnableTeam.ClassQueue.Length - 1)];
                                    spawnQueue.Add(player.Id, (player, classid));
                                }
                                catch (Exception)
                                {
                                }
                            }
                            NorthwoodLib.Pools.ListPool<Player>.Shared.Return(list);*/
                        }
                    }

                    string ttrPlayer = this.InformTTR(spectators, false, "");
                    foreach (var player in RealPlayers.List.Where(p => p.IsDead))
                    {
                        if (!DeathMessages.TryGetValue(player.Id, out string message))
                            message = "";
                        if (respawnManager._curSequence == Respawning.RespawnManager.RespawnSequencePhase.PlayingEntryAnimations)
                        {
                            /*if (NoEndlessRoundHandler.SpawnSamsara)
                                InformRespawnSamsara(ttr, respawningMTF, notrespawningMTF, spawnQueue.ContainsKey(player.Id));
                            else */
                            if (respawnManager.NextKnownTeam == Respawning.SpawnableTeamType.ChaosInsurgency)
                                message += this.InformRespawnCI(ttr, respawningCI, notrespawningCI, this.spawnQueue.ContainsKey(player.Id) ? this.spawnQueue[player.Id].Role : RoleType.None);
                            else if (respawnManager.NextKnownTeam == Respawning.SpawnableTeamType.NineTailedFox)
                                message += this.InformRespawnMTF(ttr, respawningMTF, notrespawningMTF, this.spawnQueue.ContainsKey(player.Id) ? this.spawnQueue[player.Id].Role : RoleType.None, this.spawnQueue.FirstOrDefault(i => i.Value.Role == RoleType.NtfCaptain).Value.Player?.GetDisplayName() ?? "UNKNOWN");
                            else
                                message += this.InformRespawnNone(ttr);
                        }
                        else
                            message += this.InformRespawnWaiting(ttr);
                        if (player.CheckPermissions(PlayerPermissions.AdminChat))
                        {
                            string adminMsg = "";
                            /*if (player.GetSessionVar<bool>(SessionVarType.LONG_OVERWATCH))
                                adminMsg = "[<color=red>LONG OVERWATCH <b><color=yellow>ACTIVE</color></b></color>]";
                            else if (End.OverwatchHandler.InOverwatch.TryGetValue(player.UserId, out DateTime checkTime))
                            {
                                var diff = checkTime.AddMinutes(5) - DateTime.Now;
                                adminMsg = $"[<color=yellow>OVERWATCH <b>ACTIVE</b> | {diff.Minutes:00}<color=yellow>:</color>{diff.Seconds:00}</color>]";
                            }*/
                            message += this.InformTTR(spectators, true, adminMsg);
                            message += "<br><br><br><br>" + this.InformSpectating(Player.Get(player.ReferenceHub.spectatorManager.CurrentSpectatedPlayer), true);
                        }
                        else
                        {
                            message += ttrPlayer;
                            message += "<br><br><br><br>" + this.InformSpectating(Player.Get(player.ReferenceHub.spectatorManager.CurrentSpectatedPlayer), false);
                        }

                        // player.ShowHint(message, 2);
                        player.SetGUI("specInfo", PseudoGUIPosition.MIDDLE, "<br><br><br><br><br>" + message);
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

        private string InformTTR(/*Player player, */int spectators, bool admin, string masterAdminMessage)
        {
            //string masterAdminMessage = "";//"[<color=yellow>No warning active</color>]";
            if (Round.IsLocked)
                masterAdminMessage = "[<color=yellow>ROUND LOCK <b>ACTIVE</b></color>]";
            else if (API.Utilities.Map.RespawnLock)
                masterAdminMessage = "[<color=yellow>RESPAWN LOCK <b>ACTIVE</b></color>]";
            /*else if (RealPlayers.List.Count() < 4)
                masterAdminMessage = "[<color=yellow>LESS THAN 4 PLAYERS | <b>NOT SAVING</b> ACTIVITY</color>]";*/

            var systemTimeString = string.Format(PluginHandler.Instance.Translation.TimeInfo, DateTime.Now.ToString("HH:mm:ss").Replace(":", "</color>:<color=yellow>"));
            //var deadTime = DateTime.Now - (new DateTime(player.ReferenceHub.characterClassManager.DeathTime));
            var lczTime = (float)LightContainmentZoneDecontamination.DecontaminationController.Singleton.DecontaminationPhases.First(d => d.Function == LightContainmentZoneDecontamination.DecontaminationController.DecontaminationPhase.PhaseFunction.Final).TimeTrigger - (float)LightContainmentZoneDecontamination.DecontaminationController.GetServerTime;
            var lczString = string.Format(PluginHandler.Instance.Translation.LCZInfo, ((lczTime - (lczTime % 60)) / 60).ToString("00"), Mathf.RoundToInt(lczTime % 60).ToString("00"));
            if (lczTime < 0)
                lczString = string.Format(PluginHandler.Instance.Translation.LCZInfoDecontcaminated);
            if (Warhead.IsInProgress)
                lczString = string.Format(PluginHandler.Instance.Translation.WarheadInfo, Warhead.DetonationTimer.ToString("00"));
            if (Warhead.IsDetonated)
                lczString = string.Format(PluginHandler.Instance.Translation.WarheadInfoDetonated);
            var roundTimeString = string.Format(PluginHandler.Instance.Translation.RoundInfo, Round.ElapsedTime.Minutes.ToString("00"), Round.ElapsedTime.Seconds.ToString("00"));
            var specatorString = spectators < 2 ? "Jesteś <color=yellow>jedynym</color> martwym graczem" : string.Format(PluginHandler.Instance.Translation.SpectatorInfo, spectators - 1);
            var playersString = string.Format(PluginHandler.Instance.Translation.PlayersInfo, PlayerManager.players.Count, CustomNetworkManager.slots);
            //var deadTimeString = plugin.ReadTranslation("dead_time_info", deadTime.Minutes.ToString("00"), deadTime.Seconds.ToString("00"));
            //var generatorString = plugin.ReadTranslation("generator_info", Patches.SCP079RecontainPatch.Recontained ? "5" : Map.ActivatedGenerators.ToString()) + (cache_nearestGenerator == null ? "" : $"(<color=yellow>{Math.Round(cache_nearestGenerator.remainingPowerup % 80)}</color>s)");
            //var overchargeString = plugin.ReadTranslation("overcharge_info", Patches.SCP079RecontainPatch.ErrorMode ? $"[<color=red><b>ERROR</b></color>|Code: {(Patches.SCP079RecontainPatch.Recontained ? 1 : 0)}{(Patches.SCP079RecontainPatch.Waiting ? 1 : 0)}{Patches.SCP079RecontainPatch.SecondsLeft}]" : (Exiled.Events.Handlers.CustomEvents.SCP079.IsRecontainmentPaused ? $"<color=red>{Exiled.Events.Handlers.CustomEvents.SCP079.TimeToRecontainment}</color>" : $"<color=yellow>{Exiled.Events.Handlers.CustomEvents.SCP079.TimeToRecontainment}</color>"));
            //var genString = Exiled.Events.Handlers.CustomEvents.SCP079.IsBeingRecontained ? overchargeString : generatorString;
            //if (Warhead.IsDetonated)
            //    genString = generatorString;
            var recontainmentReadyString = string.Format(PluginHandler.Instance.Translation.RecontainmentReady);
            var recontainmentNotReadyString = string.Format(PluginHandler.Instance.Translation.RecontainmentNotReady);
            var recontainmentContainedyString = string.Format(PluginHandler.Instance.Translation.RecontainmentContained);
            var recontainmentString = MapPlus.FemurBreaked ? recontainmentContainedyString : (MapPlus.Lured ? recontainmentReadyString : recontainmentNotReadyString);
            var miscString = Is106 ? recontainmentString : "[<color=yellow>REDACTED</color>]";
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
            var adminString = string.Format(PluginHandler.Instance.Translation.AdminInfo, masterAdminMessage, cache_ticketsMTF, cache_ticketsCI, adminWarheadString);
            return $"<br><br><br>{specatorString}<br><size=50%>{roundTimeString}   |   [<color=yellow>REDACTED</color>]   |   {playersString}<br>{lczString}   |   {systemTimeString}<br>{/*genString*/"[<color=yellow>REDACTED</color>]"}   |   {miscString}</size>" + (admin ? $"<br>{adminString}" : "");
        }

        private string InformSpectating(Player player, bool admin)
        {
            if (player.IsDead)
                return string.Empty;

            string tor = $"{player.GetDisplayName()} is <color=yellow>playing</color> as <color={player.RoleColor.ToHex()}>{player.Role}</color>";

            if (player.CurrentItem != null)
                tor += $"<br> and is holding {player.CurrentItem.Type}{(player.CurrentItem is Firearm firearm ? $" <color=yellow>{firearm.Ammo}</color>/<color=yellow>{firearm.MaxAmmo}</color> of <color=yellow>{firearm.AmmoType}</color> {(firearm.Aiming ? " (<color=yellow>AIMING</color>)" : string.Empty)}" : string.Empty)}";

            if (admin)
            {
                tor += $"<br>Id: {player.Id}";
            }

            return tor;
        }

        private string InformRespawnWaiting(float ttr)
        {
            return string.Format(PluginHandler.Instance.Translation.RespawnInfo, ((ttr - (ttr % 60)) / 60).ToString("00"), (ttr % 60).ToString("00"));
        }

        private string InformRespawnNone(float ttr)
        {
            return string.Format(PluginHandler.Instance.Translation.RespawnNone, (ttr % 60).ToString("00"));
        }

        private string InformRespawnSamsara(float ttr, int respawningSamsara, int notrespawningSamsara, bool willRespawn)
        {
            string roleString = willRespawn ? "<color=yellow>Przylecisz</color> jako <color=#1200ff>Jednostka Samsary</color>" : "<color=yellow><b>Nie</b> przylecisz</color>";
            return $"<color=#0096ff><size=200%><b>Helikoper Samsary łąduje</b></color> <br>za <color=yellow>{(ttr % 60):00}</color>s</size><br><color=yellow>{respawningSamsara}</color> jednostek Samsary przyleci<br><size=50%><color=yellow>{notrespawningSamsara}</color> graczy nie przyleci</size><br>{roleString}";
        }

        private string InformRespawnMTF(float ttr, int respawningMTF, int notrespawningMTF, RoleType expectedRole, string Commander)
        {
            string roleString = expectedRole == RoleType.None ? string.Format(PluginHandler.Instance.Translation.RespawnMTFWillNotRespawn, Commander) : string.Format(PluginHandler.Instance.Translation.RespawnMTFWillRespawn);
            switch (expectedRole)
            {
                case RoleType.NtfPrivate:
                    roleString += string.Format(PluginHandler.Instance.Translation.RespawnMTFWillRespawnPrivate, Commander);
                    break;
                case RoleType.NtfSergeant:
                    roleString += string.Format(PluginHandler.Instance.Translation.RespawnMTFWillRespawnSergeant, Commander);
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
