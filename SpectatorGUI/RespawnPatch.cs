using HarmonyLib;
using Respawning;
using Respawning.NamingRules;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Mistaken.SpectatorGUI;

[HarmonyPatch(typeof(RespawnManager), nameof(RespawnManager.Spawn))]
internal static class RespawnPatch
{
    public static List<ReferenceHub> PlayersToSpawn = new();

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        List<CodeInstruction> newInstructions = NorthwoodLib.Pools.ListPool<CodeInstruction>.Shared.Rent(instructions);
        int index = newInstructions.FindIndex(x => x.opcode == OpCodes.Ldftn) - 2;
        int index2 = newInstructions.FindIndex(x => x.opcode == OpCodes.Ble_S) + 22;

        Label skipLabel = generator.DefineLabel();
        var labels = newInstructions[index].ExtractLabels();
        newInstructions.RemoveRange(index, index2 - index + 1);

        newInstructions.InsertRange(index, new[]
        {
            new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(RespawnPatch), nameof(PlayersToSpawn))).WithLabels(labels),
            new CodeInstruction(OpCodes.Dup),
            new CodeInstruction(OpCodes.Stloc_1),
            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(List<ReferenceHub>), nameof(List<ReferenceHub>.Count))),
            new CodeInstruction(OpCodes.Ldc_I4_0),
            new CodeInstruction(OpCodes.Ble_S, skipLabel),
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(RespawnManager), nameof(RespawnManager.NextKnownTeam))),
            new CodeInstruction(OpCodes.Ldloca_S, 4),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UnitNamingRule), nameof(UnitNamingRule.TryGetNamingRule))),
            new CodeInstruction(OpCodes.Brfalse_S, skipLabel),
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(RespawnManager), nameof(RespawnManager.NextKnownTeam))),
            new CodeInstruction(OpCodes.Ldloc_S, 4),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UnitNameMessageHandler), nameof(UnitNameMessageHandler.SendNew))),
            new CodeInstruction(OpCodes.Nop).WithLabels(skipLabel),
        });

        foreach (var instruction in newInstructions)
            yield return instruction;

        NorthwoodLib.Pools.ListPool<CodeInstruction>.Shared.Return(newInstructions);
    }
}
