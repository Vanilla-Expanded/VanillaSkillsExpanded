using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using VSE.Passions;

namespace VSE;

public static class PrepareCarefullyPatches
{
    public delegate void UpdateSkillPassionHandler(SkillDef skill, Passion level);

    public static void Do(Harmony harm)
    {
        harm.Patch(AccessTools.Method(AccessTools.TypeByName("EdB.PrepareCarefully.PanelSkills"), "IncreasePassion"),
            new HarmonyMethod(typeof(PrepareCarefullyPatches), nameof(IncreasePassion_Prefix)));
        harm.Patch(AccessTools.Method(AccessTools.TypeByName("EdB.PrepareCarefully.PanelSkills"), "DecreasePassion"),
            new HarmonyMethod(typeof(PrepareCarefullyPatches), nameof(DecreasePassion_Prefix)));
        harm.Patch(AccessTools.Method(AccessTools.TypeByName("EdB.PrepareCarefully.PanelSkills"), "DrawPanelContent"),
            transpiler: new HarmonyMethod(typeof(PrepareCarefullyPatches), nameof(DrawPanelContent_Transpiler)));
        harm.Patch(AccessTools.Method(AccessTools.TypeByName("EdB.PrepareCarefully.PanelSkills"), "GetSkillDescription"),
            transpiler: new HarmonyMethod(typeof(PassionPatches), nameof(PassionPatches.SkillDescription_Transpiler)));
    }

    public static bool IncreasePassion_Prefix(SkillRecord record, UpdateSkillPassionHandler ___SkillPassionUpdated)
    {
        ___SkillPassionUpdated(record.def, record.passion.ChangePassion(1));
        LearnRateFactorCache.ClearCacheFor(record);
        return false;
    }

    public static bool DecreasePassion_Prefix(SkillRecord record, UpdateSkillPassionHandler ___SkillPassionUpdated)
    {
        ___SkillPassionUpdated(record.def, record.passion.ChangePassion(-1));
        LearnRateFactorCache.ClearCacheFor(record);
        return false;
    }

    public static Passion ChangePassion(this Passion passion, int offset)
    {
        var passions = PassionManager.Passions.ToList();
        passions.SortBy(def => def.learnRateFactor);
        var idx = passions.IndexOf(PassionManager.PassionToDef(passion)) + offset;
        if (idx < 0) idx += passions.Count;
        if (idx >= passions.Count) idx -= passions.Count;
        return (Passion)passions[idx].index;
    }

    public static IEnumerable<CodeInstruction> DrawPanelContent_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        var idx1 = codes.FindIndex(ins => ins.opcode == OpCodes.Ldloc_S && ins.operand is LocalBuilder { LocalIndex: 9 });
        var info = AccessTools.Field(AccessTools.TypeByName("EdB.PrepareCarefully.Textures"), "TexturePassionNone");
        var idx2 = codes.FindIndex(idx1, ins => ins.LoadsField(info));
        codes.RemoveRange(idx1 + 1, idx2 - idx1);
        codes.Insert(idx1 + 1, CodeInstruction.Call(typeof(PrepareCarefullyPatches), nameof(PassionTex)));
        return codes;
    }

    public static Texture2D PassionTex(this Passion passion) => PassionManager.PassionToDef(passion).Icon;
}