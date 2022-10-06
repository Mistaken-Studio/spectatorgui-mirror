// -----------------------------------------------------------------------
// <copyright file="RespawnPatch.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using Respawning;

#pragma warning disable IDE0060

namespace Mistaken.SpectatorGUI
{
    [HarmonyPatch(typeof(RespawnManager), "Spawn")]
    [UsedImplicitly]
    internal static class RespawnPatch
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var newInstructions = NorthwoodLib.Pools.ListPool<CodeInstruction>.Shared.Rent(instructions);
            var index = newInstructions.FindIndex(i => i.opcode == OpCodes.Stloc_3) + 1;
            index = newInstructions.FindIndex(index, i => i.opcode == OpCodes.Stloc_3) + 1;
            newInstructions.RemoveAt(index + 12 + 4);
            newInstructions.RemoveAt(index + 12 + 4);

            foreach (var t in newInstructions)
                yield return t;

            NorthwoodLib.Pools.ListPool<CodeInstruction>.Shared.Return(newInstructions);
        }
    }
}
