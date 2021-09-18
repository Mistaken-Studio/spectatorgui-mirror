// -----------------------------------------------------------------------
// <copyright file="Patch.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Respawning;

namespace Mistaken.SpectatorGUI
{
    [HarmonyPatch(typeof(RespawnManager), "Spawn")]
    internal static class RespawnPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> newInstructions = NorthwoodLib.Pools.ListPool<CodeInstruction>.Shared.Rent(instructions);
            int index = newInstructions.FindIndex((CodeInstruction i) => i.opcode == OpCodes.Stloc_3) + 1;
            index = newInstructions.FindIndex(index, (CodeInstruction i) => i.opcode == OpCodes.Stloc_3) + 1;
            newInstructions.RemoveAt(index + 12 + 4);
            newInstructions.RemoveAt(index + 12 + 4);
            for (int i = 0; i < newInstructions.Count; i++)
                yield return newInstructions[i];

            NorthwoodLib.Pools.ListPool<CodeInstruction>.Shared.Return(newInstructions);

            yield break;
        }
    }
}
