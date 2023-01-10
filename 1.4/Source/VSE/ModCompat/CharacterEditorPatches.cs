using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using VSE.Expertise;
using VSE.Passions;

namespace VSE;

public static class CharacterEditorPatches
{
    public static void Do(Harmony harm)
    {
        harm.Patch(AccessTools.Method(AccessTools.Inner(AccessTools.Inner(AccessTools.TypeByName("CharacterEditor.CEditor"), "EditorUI"), "BlockBio"),
            "ATogglePassion"), new HarmonyMethod(typeof(CharacterEditorPatches), nameof(ATogglePassion_Prefix)));
        harm.Patch(AccessTools.Method(AccessTools.Inner(AccessTools.Inner(AccessTools.TypeByName("CharacterEditor.CEditor"), "EditorUI"), "BlockBio"),
            "ARandomSkills"), transpiler: new HarmonyMethod(typeof(CharacterEditorPatches), nameof(ARandomSkills_Transpiler)));
        harm.Patch(AccessTools.Method(AccessTools.Inner(AccessTools.Inner(AccessTools.TypeByName("CharacterEditor.CEditor"), "EditorUI"), "BlockBio"),
                "DrawSkills"), new HarmonyMethod(typeof(CharacterEditorPatches), nameof(DrawSkills_Prefix)),
            new HarmonyMethod(typeof(CharacterEditorPatches), nameof(DrawSkills_Postfix)));
        harm.Patch(AccessTools.Method(AccessTools.Inner(AccessTools.Inner(AccessTools.TypeByName("CharacterEditor.CEditor"), "EditorUI"), "BlockPerson"),
            "ARandomizeBio"), transpiler: new HarmonyMethod(typeof(CharacterEditorPatches), nameof(ARandomSkills_Transpiler)));
        harm.Patch(AccessTools.Method(AccessTools.TypeByName("CharacterEditor.SkillTool"), "SetSkillsFromSeparatedString"),
            transpiler: new HarmonyMethod(typeof(CharacterEditorPatches), nameof(SetSkillsAsSeparatedString_Transpiler)));
        harm.Patch(AccessTools.Method(AccessTools.TypeByName("CharacterEditor.SkillTool"), "GetSkillAsSeparatedString"),
            transpiler: new HarmonyMethod(typeof(CharacterEditorPatches), nameof(GetSkillAsSeparatedString_Transpiler)));
    }

    public static bool ATogglePassion_Prefix(SkillRecord record)
    {
        Find.WindowStack.Add(new FloatMenu(DefDatabase<PassionDef>.AllDefs.Select(passion =>
                new FloatMenuOption(passion.LabelCap, () =>
                {
                    LearnRateFactorCache.ClearCacheFor(record, (Passion)passion.index);
                    record.passion = (Passion)passion.index;
                }, passion.Icon, Color.white))
           .ToList()));
        return false;
    }

    public static IEnumerable<CodeInstruction> ARandomSkills_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        var info = AccessTools.Field(typeof(SkillRecord), nameof(SkillRecord.passion));
        var idx1 = codes.FindIndex(ins => ins.StoresField(info));
        var idx2 = codes.FindLastIndex(idx1, ins => ins.opcode == OpCodes.Ldc_I4_0);
        codes.RemoveRange(idx2, idx1 - idx2);
        codes.Insert(idx2, CodeInstruction.Call(typeof(CharacterEditorPatches), nameof(RandomPassion)));
        codes.Insert(idx2, new CodeInstruction(OpCodes.Pop));
        return codes;
    }

    public static Passion RandomPassion() => (Passion)DefDatabase<PassionDef>.AllDefs.RandomElement().index;

    public static void DrawSkills_Prefix()
    {
        ExpertisePatches.ForceVanillaSkills = true;
    }

    public static void DrawSkills_Postfix()
    {
        ExpertisePatches.ForceVanillaSkills = false;
    }

    public static IEnumerable<CodeInstruction> SetSkillsAsSeparatedString_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        var idx1 = codes.FindIndex(ins => ins.opcode == OpCodes.Ldc_I4_2);
        var info = AccessTools.Method(AccessTools.TypeByName("CharacterEditor.Extension"), "AsInt32");
        var idx2 = codes.FindIndex(idx1, ins => ins.Calls(info));
        var idx3 = codes.FindIndex(idx2, ins => ins.opcode == OpCodes.Conv_U1);
        codes.RemoveRange(idx2, idx3 - idx2 + 1);
        codes.Insert(idx2, CodeInstruction.Call(typeof(CharacterEditorPatches), nameof(GetPassionFromString)));
        return codes;
    }

    public static Passion GetPassionFromString(string str)
    {
        if (int.TryParse(str, out var num)) return (Passion)num;
        return (Passion)(DefDatabase<PassionDef>.GetNamedSilentFail(str) ?? PassionDefOf.None).index;
    }

    public static IEnumerable<CodeInstruction> GetSkillAsSeparatedString_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        var info = AccessTools.Field(typeof(SkillRecord), nameof(SkillRecord.passion));
        var idx1 = codes.FindIndex(ins => ins.LoadsField(info));
        var idx2 = codes.FindIndex(idx1, ins => ins.opcode == OpCodes.Stloc_3);
        codes.RemoveRange(idx2, 3);
        codes.Insert(idx2, CodeInstruction.Call(typeof(CharacterEditorPatches), nameof(GetStringFromPassion)));
        return codes;
    }

    public static string GetStringFromPassion(Passion passion) => PassionManager.PassionToDef(passion).defName;
}
