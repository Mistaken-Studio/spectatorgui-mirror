namespace Mistaken.SpectatorGUI;

internal sealed class Translations
{
    public string LCZInfo { get; set; } = "LCZ decontamination in <color=yellow>{0}</color>m <color=yellow>{1}</color>s";

    public string LCZInfoDecontcaminated { get; set; } = "LCZ <color=yellow>DECONTAMINATED</color>";

    public string WarheadInfo { get; set; } = "Warhead detonation in proggress <color=yellow>{0}</color>s";

    public string WarheadInfoDetonated { get; set; } = "Warhead <color=yellow>detonated</color>";

    public string RoundInfo { get; set; } = "Runda trwa już <color=yellow>{0}</color>m <color=yellow>{1}</color>s";

    public string RespawnInfo { get; set; } = "<size=150%>Wsparcie przybędzie za <color=yellow>{0}</color>m <color=yellow>{1}</color>s</size><br><br><br><br><br><br><br>";

    public string SpectatorInfo { get; set; } = "Jesteś obserwatorem z <color=yellow>{0}</color> innymi graczami";

    public string OnlySpectatorInfo { get; set; } = "Jesteś <color=yellow>jedynym</color> obserwatorem";

    public string PlayersInfo { get; set; } = "Liczba graczy: <color=yellow>{0}</color>/<color=yellow>{1}</color>";

    public string GeneratorInfo { get; set; } = "Generatory: <color=yellow>{0}</color>/<color=yellow>3</color>";

    public string OverchargeInfo { get; set; } = "Status SCP-079: {0}";

    public string AdminWarheadInfo { get; set; } = "Starter: <color=yellow>({0}) {1}</color>   |   <color=yellow>({2}) {3}</color> :Stoper";

    public string AdminInfo { get; set; } = "{0}<br><size=50%>MTF: <color=yellow>{1}</color>   |   <color=yellow>{2}</color> :CI</size><br>";

    public string RespawnNone { get; set; } = "<color=#8f0000><b>Nikt? zrespi się</b></color> za <color=yellow>{0}</color>s";

    public string RespawnCIWillRespawnRifleman { get; set; } = "<color=#097c1b>Strzelec Rebelii Chaosu</color>";

    public string RespawnCIWillRespawnRepressor { get; set; } = "<color=#0d7d35>Represor Rebelii Chaosu</color>";

    public string RespawnCIWillRespawnMarauder { get; set; } = "<color=#006826>Maruder Rebelii Chaosu</color>";

    public string RespawnCIWillRespawn { get; set; } = "<color=yellow>Zrespisz się</color> jako ";

    public string RespawnCIWillNotRespawn { get; set; } = "<color=yellow><b>Nie</b> zrespisz się</color>";

    public string RespawnCIRespawn { get; set; } = "<color=#1d6f00><size=200%><b>🚙 Samochód przybywa 🚙</b></color> za <color=yellow>{0}</size>s</color><br><color=yellow>{1}</color> CI przybędzie<br><size=50%><color=yellow>{2}</color> Graczy nie zrespi się</size><br>{3}";

    public string RespawnMTFWillRespawnPrivate { get; set; } = "<color=#61beff>Szeregowy Nine-TailedFox</color><br>Twoim <color=#1200ff>Kapitanem</color> <color=yellow>będzie</color> {0}";

    public string RespawnMTFWillRespawnSergeant { get; set; } = "<color=#0096ff>Sierżant Nine-Tailed Fox</color><br>Twoim <color=#1200ff>Kapitanem</color> <color=yellow>będzie</color> {0}";

    public string RespawnMTFWillRespawnCaptain { get; set; } = "<color=#1200ff>Kapitan Nine-Tailed Fox</color>";

    public string RespawnMTFWillRespawn { get; set; } = "<color=yellow>Zrespisz się</color> jako ";

    public string RespawnMTFWillNotRespawn { get; set; } = "<color=yellow><b>Nie</b> zrespisz się</color><br><color=#1200ff>Kapitanem</color><color=yellow>będzie</color> {0}";

    public string RespawnMTFRespawn { get; set; } = "<color=#0096ff><size=200%><b>🚁 Helikopter ląduje 🚁</b></color> za <color=yellow>{0}</color>s</size><br><color=yellow>{1}</color> MFO przybędzie<br><size=50%><color=yellow>{2}</color> Graczy nie zrespi się</size><br>{3}";
}
