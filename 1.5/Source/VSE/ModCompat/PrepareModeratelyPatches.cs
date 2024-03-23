using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using VSE.Passions;

namespace VSE;

public static class PrepareModeratelyPatches
{
    public static void Do(Harmony harm)
    {
        harm.Patch(AccessTools.Method(typeof(FloatMenuUtility), nameof(FloatMenuUtility.MakeMenu), generics: new[] { typeof(Passion) }),
            new HarmonyMethod(typeof(PrepareModeratelyPatches), nameof(MakeMenu_Prefix)));
        harm.Patch(
            AccessTools.Method(AccessTools.TypeByName("Lakuna.PrepareModerately.Filter.Part.PawnFilterPart"), "GetRandomOfEnum",
                generics: new[] { typeof(Passion) }), new HarmonyMethod(typeof(PrepareModeratelyPatches), nameof(GetRandomOfEnum_Prefix)));
        harm.Patch(
            AccessTools.Method(AccessTools.TypeByName("Lakuna.PrepareModerately.Filter.Part.Types.HasPassionsAtLevel"), "DoEditInterface"),
            transpiler: new HarmonyMethod(typeof(PrepareModeratelyPatches), nameof(PassionStringTranspiler)));
        harm.Patch(
            AccessTools.Method(AccessTools.TypeByName("Lakuna.PrepareModerately.Filter.Part.Types.HasPassionsAtLevel"), "Summary"),
            transpiler: new HarmonyMethod(typeof(PrepareModeratelyPatches), nameof(PassionStringTranspiler)));
        harm.Patch(
            AccessTools.Method(AccessTools.TypeByName("Lakuna.PrepareModerately.Filter.Part.Types.HasPassion"), "DoEditInterface"),
            transpiler: new HarmonyMethod(typeof(PrepareModeratelyPatches), nameof(PassionStringTranspiler)));
        harm.Patch(
            AccessTools.Method(AccessTools.TypeByName("Lakuna.PrepareModerately.Filter.Part.Types.HasPassion"), "Summary"),
            transpiler: new HarmonyMethod(typeof(PrepareModeratelyPatches), nameof(PassionStringTranspiler)));
    }

    public static void MakeMenu_Prefix(ref IEnumerable<Passion> objects, ref Func<Passion, string> labelGetter)
    {
        objects = PassionManager.AllPassions;
        labelGetter = passion => PassionManager.PassionToDef(passion).LabelCap;
    }

    public static void GetRandomOfEnum_Prefix(ref Passion __result)
    {
        __result = PassionManager.AllPassions.RandomElement();
    }

    public static IEnumerable<CodeInstruction> PassionStringTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        var info1 = AccessTools.Method(typeof(object), nameof(ToString));
        var info2 = AccessTools.Method(typeof(PrepareModeratelyPatches), nameof(PassionToString));
        for (var i = 0; i < codes.Count; i++)
            if (codes[i].opcode == OpCodes.Ldflda && codes[i + 1].opcode == OpCodes.Constrained && codes[i + 1].OperandIs(typeof(Passion)) &&
                codes[i + 2].Calls(info1))
            {
                yield return new CodeInstruction(OpCodes.Ldfld, codes[i].operand);
                yield return new CodeInstruction(OpCodes.Call, info2);
                i += 2;
            }
            else yield return codes[i];
    }

    public static string PassionToString(Passion passion) => PassionManager.PassionToDef(passion).label;
}