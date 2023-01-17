using HarmonyLib;
using Respawning;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Mistaken.SpectatorGUI;

[HarmonyPatch(typeof(RespawnManager), "Spawn")]
internal static class RespawnPatch
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        List<CodeInstruction> newInstructions = NorthwoodLib.Pools.ListPool<CodeInstruction>.Shared.Rent(instructions);
        int index = newInstructions.FindIndex((CodeInstruction i) => i.opcode == OpCodes.Ldloc_S) + 3;

        newInstructions.RemoveAt(index);
        newInstructions.InsertRange(index, new CodeInstruction[]
        {
            new(OpCodes.Ldsfld, AccessTools.Field(typeof(SpectatorInfoHandler), "_respawnQueueSeed")),
            new(OpCodes.Call, AccessTools.Method(typeof(SpectatorInfoHandler), "ShuffleList").MakeGenericMethod(typeof(List<ReferenceHub>).MakeByRefType())),
        });

        foreach (var instruction in newInstructions)
            yield return instruction;

        NorthwoodLib.Pools.ListPool<CodeInstruction>.Shared.Return(newInstructions);
    }
}
