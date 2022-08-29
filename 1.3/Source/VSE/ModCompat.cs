using System;
using HarmonyLib;
using MonoMod.Utils;
using Verse;

namespace VSE;

public static class ModCompat
{
    private static AccessTools.FieldRef<object, object> insaneSetting;
    private static AccessTools.FieldRef<object, int> valueSkillCap;
    private static Func<float> saturatedXPMultiplier;

    public static bool InsaneSkills;
    public static bool CharacterEditor;
    public static bool PrepareCarefully;
    public static bool MadSkills;

    public static float SaturatedXPMultiplier => saturatedXPMultiplier();

    public static float ValueSkillCap => valueSkillCap(insaneSetting());

    public static void Init()
    {
        InsaneSkills = ModLister.HasActiveModWithName("Ducks' Insane Skills");
        CharacterEditor = ModLister.HasActiveModWithName("Character Editor");
        PrepareCarefully = ModLister.HasActiveModWithName("EdB Prepare Carefully");
        MadSkills = ModLister.HasActiveModWithName("Mad Skills");

        if (InsaneSkills)
        {
            insaneSetting = AccessTools.FieldRefAccess<object>("DucksInsaneSkills.DucksInsaneSkillsMod:settings");
            valueSkillCap = AccessTools.FieldRefAccess<int>("DucksInsaneSkills.DucksInsaneSkillsSettings:ValueSkillCap");
            InsaneSkillsPatches.Do(SkillsMod.Harm);
        }

        if (CharacterEditor) CharacterEditorPatches.Do(SkillsMod.Harm);

        if (PrepareCarefully) PrepareCarefullyPatches.Do(SkillsMod.Harm);

        if (MadSkills)
            saturatedXPMultiplier = AccessTools.PropertyGetter(AccessTools.TypeByName("RTMadSkills.ModSettings"), "saturatedXPMultiplier")
                .CreateDelegate<Func<float>>();
    }
}