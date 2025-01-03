using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using VSE.Passions;

namespace VSE;

public static class PrepareCarefullyPatches
{
    public static void Do(Harmony harm)
    {
        harm.Patch(AccessTools.Method(AccessTools.TypeByName("EdB.PrepareCarefully.ManagerPawns"), "UpdatePawnSkillPassion"),
            transpiler: new HarmonyMethod(typeof(PrepareCarefullyPatches), nameof(UpdatePawnSkillPassion_Transpiler)));
    }
    
    public static IEnumerable<CodeInstruction> UpdatePawnSkillPassion_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var passionField = AccessTools.Field(typeof(SkillRecord), nameof(SkillRecord.passion));
        var codes = instructions.ToList();
        var idx1 = codes.FindIndex(ins => ins.StoresField(passionField));
        codes.RemoveAt(idx1);
        codes.Insert(idx1, CodeInstruction.Call(typeof(PrepareCarefullyPatches), nameof(UpdatePawnSkillPassionAndClearCache)));
        return codes;
    }

    public static void UpdatePawnSkillPassionAndClearCache(SkillRecord skillRecord, Passion passion)
    {
        LearnRateFactorCache.ClearCacheFor(skillRecord, passion);
        skillRecord.passion = passion;
    }

    public static Texture2D PassionTex(this Passion passion) => PassionManager.PassionToDef(passion).Icon;
}