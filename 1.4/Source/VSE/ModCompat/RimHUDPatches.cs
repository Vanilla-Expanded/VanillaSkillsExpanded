using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using VSE.Passions;

namespace VSE;

public static class RimHUDPatches
{
    private static RHDelegate getMinor;
    private static RHDelegate getMajor;
    private static RHDelegate getDisabled;
    private static RHDelegate getMain;

    public static void Do(Harmony harm)
    {
        var minor = AccessTools.MethodDelegate<ColourDelegate>(AccessTools.PropertyGetter("RimHUD.Data.Configuration.Theme:SkillMinorPassionColor"));
        var major = AccessTools.MethodDelegate<ColourDelegate>(AccessTools.PropertyGetter("RimHUD.Data.Configuration.Theme:SkillMajorPassionColor"));
        var disabled = AccessTools.MethodDelegate<ColourDelegate>(AccessTools.PropertyGetter("RimHUD.Data.Configuration.Theme:DisabledColor"));
        var mainText = AccessTools.MethodDelegate<ColourDelegate>(AccessTools.PropertyGetter("RimHUD.Data.Configuration.Theme:MainTextColor"));
        var value = AccessTools.PropertyGetter("RimHUD.Data.Configuration.ColorOption:Value");
        getMinor = AccessTools.MethodDelegate<RHDelegate>(value, minor());
        getMajor = AccessTools.MethodDelegate<RHDelegate>(value, major());
        getDisabled = AccessTools.MethodDelegate<RHDelegate>(value, disabled());
        getMain = AccessTools.MethodDelegate<RHDelegate>(value, mainText());
        harm.Patch(AccessTools.Constructor(AccessTools.TypeByName("RimHUD.Data.Models.SkillModel"),
                new[] { AccessTools.TypeByName("RimHUD.Data.Models.PawnModel"), typeof(SkillDef) }),
            transpiler: new HarmonyMethod(typeof(RimHUDPatches), nameof(Constructor_Transpiler)));
        harm.Patch(AccessTools.Method(AccessTools.TypeByName("SkillModel"), "GetSkillColor"),
            new HarmonyMethod(typeof(RimHUDPatches), nameof(GetSkillColor_Prefix)));
    }

    public static bool GetSkillColor_Prefix(ref Color __result, SkillRecord skill)
    {
        __result = PassionManager.PassionToDef(skill.passion).color switch
        {
            PassionColor.Disabled => getDisabled(),
            PassionColor.Main => getMain(),
            PassionColor.Minor => getMinor(),
            PassionColor.Major => getMajor(),
            _ => getMain()
        };
        return false;
    }

    public static IEnumerable<CodeInstruction> Constructor_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        var info2 = AccessTools.Method(typeof(object), nameof(ToString));
        var startIndex = codes.FindIndex(ins => ins.opcode == OpCodes.Newobj);
        Log.Message($"startIndex: {startIndex}");
        var endIndex = codes.FindIndex(startIndex, ins => ins.Calls(info2));
        Log.Message($"endIndex: {endIndex}");
        Log.Message($"Start: {codes[startIndex]}\nEnd:{codes[endIndex]}");
        codes.RemoveRange(startIndex, endIndex - startIndex + 1);
        Log.Message($"Start: {codes[startIndex]}\nEnd:{codes[endIndex]}");
        codes.InsertRange(startIndex, new List<CodeInstruction>
        {
            new(OpCodes.Ldloc_0),
            new(OpCodes.Ldfld, AccessTools.Field(typeof(SkillRecord), nameof(SkillRecord.passion))),
            new(OpCodes.Call, AccessTools.Method(typeof(PassionManager), nameof(PassionManager.PassionToDef))),
            new(OpCodes.Call, AccessTools.PropertyGetter(typeof(PassionDef), nameof(PassionDef.Indicator)))
        });

        return codes;
    }

    // these grab the rimhud colours from settings
    private delegate object ColourDelegate();

    // these make the colours usable
    private delegate Color RHDelegate();
}
