// -----------------------------------------------------------------------
// <copyright file="Translation.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Exiled.API.Interfaces;

namespace Mistaken.SpectatorGUI
{
    internal class Translation : ITranslation
    {
        public string TimeInfo { get; set; } = "Current time: <color=yellow>{0}</color>";

        public string LCZInfo { get; set; } = "Light Containment Zone decontamination in <color=yellow>{0}</color>m <color=yellow>{1}</color>s";

        public string LCZInfoDecontcaminated { get; set; } = "Light Containment Zone <color=yellow>DECONTAMINATED</color>";

        public string WarheadInfo { get; set; } = "Warhead detonation in proggress <color=yellow>{0}</color>s";

        public string WarheadInfoDetonated { get; set; } = "Warhead <color=yellow>detonated</color>";

        public string RoundInfo { get; set; } = "Round is <color=yellow>{0}</color>m <color=yellow>{1}</color>s long";

        public string RespawnInfo { get; set; } = "<size=150%>Respawn in <color=yellow>{0}</color>m <color=yellow>{1}</color>s</size>";

        public string SpectatorInfo { get; set; } = "You are spectator with <color=yellow>{0}</color> other players";

        public string OnlySpectatorInfo { get; set; } = "You are <color=yellow>alone</color> spectator";

        public string PlayersInfo { get; set; } = "Server has <color=yellow>{0}</color> for max <color=yellow>{1}</color> players";

        public string DeadTimeInfo { get; set; } = "You are dead for <color=yellow>{0}</color>m <color=yellow>{1}</color>s";

        public string GeneratorInfo { get; set; } = "Generators: <color=yellow>{0}</color>/<color=yellow>3</color>";

        public string OverchargeInfo { get; set; } = "SCP 079 status: {0}";

        public string AdminWarheadInfo { get; set; } = "Warhead status: {0}   |   Warhead Button Open: <color=yellow>{1}</color><br>Warhead Last Starter: <color=yellow>({2}) {3}</color>   |   <color=yellow>({4}) {5}</color> :Warhead Last Stoper<br>Warhead Lock Start: <color=yellow>{6}</color>   |   <color=yellow>{7}</color> :Warhead Lock Stop";

        public string AdminInfo { get; set; } = "{0}<br><size=50%>MTF Tickets: <color=yellow>{1}</color>   |   <color=yellow>{2}</color> :CI Tickets</size><br><size=50%>{3}</size>";

        public string RecontainmentNotReady { get; set; } = "SCP 106 <color=yellow><b>not</b> ready</color> for recontainment";

        public string RecontainmentReady { get; set; } = "SCP 106 <color=yellow>ready</color> for recontainment";

        public string RecontainmentContained { get; set; } = "SCP 106 <color=yellow>recontained</color>";

        public string RespawnNone { get; set; } = "<color=#8f0000><b>None? will respawn</b></color> in <color=yellow>{0}</color>s";

        public string RespawnCIWillRespawnRifleman { get; set; } = "<color=#097c1b>Chaos Insurgency Rifleman</color>";

        public string RespawnCIWillRespawnRepressor { get; set; } = "<color=#0d7d35>Chaos Insurgency Repressor</color>";

        public string RespawnCIWillRespawnMarauder { get; set; } = "<color=#006826>Chaos Insurgency Maruder</color>";

        public string RespawnCIWillRespawn { get; set; } = "You <color=yellow>will</color> respawn as <color=#1d6f00>Chaos Insurgent</color>";

        public string RespawnCIWillNotRespawn { get; set; } = "You <color=yellow>will <b>not</b></color> respawn";

        public string RespawnCIRespawn { get; set; } = "<color=#1d6f00><size=200%><b>Car is arriving</b></color> in <color=yellow>{0}</size>s</color><br><color=yellow>{1}</color> Insurgents will respawn<br><size=50%><color=yellow>{2}</color> players will not respawn</size><br>{3}";

        public string RespawnMTFWillRespawnPrivate { get; set; } = "<color=#61beff>Ninetailedfox Private</color><br>Your <color=#1200ff>Commander</color> <color=yellow>will</color> be {0}";

        public string RespawnMTFWillRespawnSergeant { get; set; } = "<color=#0096ff>Ninetailedfox Sergeant</color><br>Your <color=#1200ff>Commander</color> <color=yellow>will</color> be {0}";

        public string RespawnMTFWillRespawnCaptain { get; set; } = "<color=#1200ff>Ninetailedfox Captain</color>";

        public string RespawnMTFWillRespawn { get; set; } = "You <color=yellow>will</color> respawn as ";

        public string RespawnMTFWillNotRespawn { get; set; } = "You <color=yellow>will <b>not</b></color> respawn<br><color=#1200ff>Commander</color> <color=yellow>will</color> be {0}";

        public string RespawnMTFRespawn { get; set; } = "<color=#0096ff><size=200%><b>Helicopter is landing</b></color> <br>in <color=yellow>{0}</color>s</size><br><color=yellow>{1}</color> Ninetailefox will respawn<br><size=50%><color=yellow>{2}</color> players will not respawn</size><br>{3}";
    }
}
