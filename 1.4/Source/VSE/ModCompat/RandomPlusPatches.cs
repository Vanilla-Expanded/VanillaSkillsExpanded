using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using VSE.Passions;

namespace VSE;

public static class RandomPlusPatches
{
    public static void Do(Harmony harm)
    {
        harm.Patch(AccessTools.Method(AccessTools.TypeByName("RandomPlus.PanelSkills"), "DrawPanelContent"),
            transpiler: new HarmonyMethod(typeof(RandomPlusPatches), nameof(DrawPanelContent_Transpiler)));
        harm.Patch(AccessTools.Method(AccessTools.TypeByName("RandomPlus.PanelSkills"), "IncreasePassion"),
            new HarmonyMethod(typeof(RandomPlusPatches), nameof(IncreasePassion_Prefix)));
        harm.Patch(AccessTools.Method(AccessTools.TypeByName("RandomPlus.PanelSkills"), "DecreasePassion"),
            new HarmonyMethod(typeof(RandomPlusPatches), nameof(DecreasePassion_Prefix)));
        harm.Patch(AccessTools.Method(AccessTools.TypeByName("RandomPlus.RandomSettings"), "CheckSkillsIsSatisfied"),
            transpiler: new HarmonyMethod(typeof(RandomPlusPatches), nameof(CheckSkillsIsSatisfied_Transpiler)));
    }

    public static IEnumerable<CodeInstruction> DrawPanelContent_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        var info1 = AccessTools.PropertyGetter(AccessTools.TypeByName("RandomPlus.SkillContainer"), "Passion");
        var idx1 = codes.FindIndex(ins => ins.Calls(info1));
        var info = AccessTools.Field(AccessTools.TypeByName("RandomPlus.Textures"), "TexturePassionNone");
        var idx2 = codes.FindIndex(idx1, ins => ins.LoadsField(info));
        codes.RemoveRange(idx1 + 1, idx2 - idx1);
        codes.Insert(idx1 + 1, CodeInstruction.Call(typeof(PrepareCarefullyPatches), nameof(PrepareCarefullyPatches.PassionTex)));
        return codes;
    }

    public static bool IncreasePassion_Prefix(object __0)
    {
        ModCompat.SetPassion(__0, ModCompat.GetPassion(__0).ChangePassion(1));
        return false;
    }

    public static bool DecreasePassion_Prefix(object __0)
    {
        ModCompat.SetPassion(__0, ModCompat.GetPassion(__0).ChangePassion(-1));
        return false;
    }

    public static IEnumerable<CodeInstruction> CheckSkillsIsSatisfied_Transpiler(IEnumerable<CodeInstruction> instructions) =>
        PassionPatches.CompareReplacer(instructions, ins => ins.opcode == OpCodes.Blt_S);
}