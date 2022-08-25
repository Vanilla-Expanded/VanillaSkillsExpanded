using HarmonyLib;
using Verse;

namespace VSE;

public static class ModCompat
{
    private static AccessTools.FieldRef<object, object> insaneSetting;
    private static AccessTools.FieldRef<object, int> valueSkillCap;

    public static bool InsaneSkills;
    public static bool CharacterEditor;
    public static bool PrepareCarefully;

    public static float ValueSkillCap => valueSkillCap(insaneSetting());

    public static void Init()
    {
        InsaneSkills = ModLister.HasActiveModWithName("Ducks' Insane Skills");
        CharacterEditor = ModLister.HasActiveModWithName("Character Editor");
        PrepareCarefully = ModLister.HasActiveModWithName("EdB Prepare Carefully");

        if (InsaneSkills)
        {
            insaneSetting = AccessTools.FieldRefAccess<object>("DucksInsaneSkills.DucksInsaneSkillsMod:settings");
            valueSkillCap = AccessTools.FieldRefAccess<int>("DucksInsaneSkills.DucksInsaneSkillsSettings:ValueSkillCap");
            InsaneSkillsPatches.Do(SkillsMod.Harm);
        }

        if (CharacterEditor) CharacterEditorPatches.Do(SkillsMod.Harm);

        if (PrepareCarefully) PrepareCarefullyPatches.Do(SkillsMod.Harm);
    }
}