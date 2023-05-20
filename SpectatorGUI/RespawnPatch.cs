using HarmonyLib;
using Respawning;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Mistaken.SpectatorGUI;

[HarmonyPatch(typeof(RespawnManager), nameof(RespawnManager.Spawn))]
internal static class RespawnPatch
{
    public static int Seed = -1;

    public static void ShuffleList<T>(IList<T> list, int seed)
    {
        Random random = new(seed);
        int i = list.Count;
        while (i > 1)
        {
            i--;
            int index = random.Next(i + 1);
            (list[i], list[index]) = (list[index], list[i]);
        }
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        List<CodeInstruction> newInstructions = NorthwoodLib.Pools.ListPool<CodeInstruction>.Shared.Rent(instructions);

        int index = newInstructions.FindIndex(x => x.opcode == OpCodes.Br_S) + 1;

        List<Label> labels1 = newInstructions[index].ExtractLabels();
        newInstructions.RemoveRange(index, 2);
        newInstructions[index].WithLabels(labels1);

        int index2 = newInstructions.FindIndex(x => x.opcode == OpCodes.Ldloc_S) + 2;

        List<Label> labels2 = newInstructions[index2].ExtractLabels();
        newInstructions.RemoveRange(index2, 2);
        newInstructions[index2].WithLabels(labels2);

        Label label3 = generator.DefineLabel();

        newInstructions.InsertRange(index, new[]
        {
            new CodeInstruction(OpCodes.Ldloc_1),
            new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(RespawnPatch), nameof(Seed))),
            new CodeInstruction(OpCodes.Dup),
            new CodeInstruction(OpCodes.Ldc_I4_M1),
            new CodeInstruction(OpCodes.Cgt),
            new CodeInstruction(OpCodes.Brtrue_S, label3),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Ldc_I4_0),
            new CodeInstruction(OpCodes.Ldc_I4, int.MaxValue),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.Random), nameof(UnityEngine.Random.Range), new Type[] { typeof(int), typeof(int) })),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RespawnPatch), nameof(ShuffleList)).MakeGenericMethod(typeof(ReferenceHub))).WithLabels(label3),
        });

        foreach (var instruction in newInstructions)
            yield return instruction;

        NorthwoodLib.Pools.ListPool<CodeInstruction>.Shared.Return(newInstructions);
    }
}
