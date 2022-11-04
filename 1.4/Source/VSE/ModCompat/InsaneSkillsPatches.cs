using System.Reflection;
using HarmonyLib;
using RimWorld;
using VSE.Passions;

namespace VSE;

public static class InsaneSkillsPatches
{
    private static bool enableSkillLoss;
    private static MethodInfo targetMethod;
    private static MethodInfo patchMethod;

    public static void Do(Harmony harm)
    {
        harm.Patch(AccessTools.Method(AccessTools.TypeByName("DucksInsaneSkills.DucksSkills_Learn"), "Prefix"),
            transpiler: new HarmonyMethod(typeof(PassionPatches), nameof(PassionPatches.Learn_Transpiler)));
        targetMethod = AccessTools.Method(typeof(SkillRecord), "Interval");
        patchMethod = AccessTools.Method(AccessTools.TypeByName("DucksInsaneSkills.DucksSkills_Interval"), "Prefix");
    }

    public static void UpdateSkillLoss(bool enable, Harmony harm)
    {
        if (enable == enableSkillLoss) return;
        if (enable)
            harm.Unpatch(targetMethod, patchMethod);
        else
            harm.Patch(targetMethod, new HarmonyMethod(patchMethod));

        enableSkillLoss = enable;
    }
}