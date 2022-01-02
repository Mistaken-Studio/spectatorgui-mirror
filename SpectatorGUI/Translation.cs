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
        public string LCZInfo { get; set; } = "LCZ decontamination in <color=yellow>{0}</color>m <color=yellow>{1}</color>s";

        public string LCZInfoDecontcaminated { get; set; } = "LCZ <color=yellow>DECONTAMINATED</color>";

        public string WarheadInfo { get; set; } = "Warhead detonation in proggress <color=yellow>{0}</color>s";

        public string WarheadInfoDetonated { get; set; } = "Warhead <color=yellow>detonated</color>";

        public string RoundInfo { get; set; } = "Round is <color=yellow>{0}</color>m <color=yellow>{1}</color>s long";

        public string RespawnInfo { get; set; } = "<size=150%>Respawn in <color=yellow>{0}</color>m <color=yellow>{1}</color>s</size><br><br><br><br><br><br><br>";

        public string SpectatorInfo { get; set; } = "You are spectator with <color=yellow>{0}</color> other players";

        public string OnlySpectatorInfo { get; set; } = "You are <color=yellow>alone</color> spectator";

        public string PlayersInfo { get; set; } = "Players: <color=yellow>{0}</color>/<color=yellow>{1}</color>";

        public string GeneratorInfo { get; set; } = "Generators: <color=yellow>{0}</color>/<color=yellow>3</color>";

        public string OverchargeInfo { get; set; } = "SCP 079 status: {0}";

        public string AdminWarheadInfo { get; set; } = "Starter: <color=yellow>({0}) {1}</color>   |   <color=yellow>({2}) {3}</color> :Stoper";

        public string AdminInfo { get; set; } = "{0}<br><size=50%>MTF: <color=yellow>{1}</color>   |   <color=yellow>{2}</color> :CI</size><br><size=50%>{3}</size>";

        public string RespawnNone { get; set; } = "<color=#8f0000><b>None? will respawn</b></color> in <color=yellow>{0}</color>s";

        public string RespawnCIWillRespawnRifleman { get; set; } = "<color=#097c1b>Chaos Insurgency Rifleman</color>";

        public string RespawnCIWillRespawnRepressor { get; set; } = "<color=#0d7d35>Chaos Insurgency Repressor</color>";

        public string RespawnCIWillRespawnMarauder { get; set; } = "<color=#006826>Chaos Insurgency Maruder</color>";

        public string RespawnCIWillRespawn { get; set; } = "You <color=yellow>will</color> respawn as ";

        public string RespawnCIWillNotRespawn { get; set; } = "You <color=yellow>will <b>not</b></color> respawn";

        public string RespawnCIRespawn { get; set; } = "<color=#1d6f00><size=200%><b>🚙 Car is arriving 🚙</b></color> in <color=yellow>{0}</size>s</color><br><color=yellow>{1}</color> Insurgents will respawn<br><size=50%><color=yellow>{2}</color> players will not respawn</size><br>{3}";

        public string RespawnMTFWillRespawnPrivate { get; set; } = "<color=#61beff>Ninetailedfox Private</color><br>Your <color=#1200ff>Commander</color> <color=yellow>will</color> be {0}";

        public string RespawnMTFWillRespawnSergeant { get; set; } = "<color=#0096ff>Ninetailedfox Sergeant</color><br>Your <color=#1200ff>Commander</color> <color=yellow>will</color> be {0}";

        public string RespawnMTFWillRespawnCaptain { get; set; } = "<color=#1200ff>Ninetailedfox Captain</color>";

        public string RespawnMTFWillRespawn { get; set; } = "You <color=yellow>will</color> respawn as ";

        public string RespawnMTFWillNotRespawn { get; set; } = "You <color=yellow>will <b>not</b></color> respawn<br><color=#1200ff>Commander</color> <color=yellow>will</color> be {0}";

        public string RespawnMTFRespawn { get; set; } = "<color=#0096ff><size=200%><b>🚁 Helicopter is landing 🚁</b></color> in <color=yellow>{0}</color>s</size><br><color=yellow>{1}</color> Ninetailefox will respawn<br><size=50%><color=yellow>{2}</color> players will not respawn</size><br>{3}";
    }
}
