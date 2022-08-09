using HarmonyLib;

namespace VSE;

public static class ModCompat
{
    private static AccessTools.FieldRef<object, object> insaneSetting;
    private static AccessTools.FieldRef<object, int> valueSkillCap;

    public static float ValueSkillCap => valueSkillCap(insaneSetting());

    public static void Init()
    {
        if (SkillsMod.InsaneSkills)
        {
            insaneSetting = AccessTools.FieldRefAccess<object>("DucksInsaneSkills.DucksInsaneSkillsMod:settings");
            valueSkillCap = AccessTools.FieldRefAccess<int>("DucksInsaneSkills.DucksInsaneSkillsSettings:ValueSkillCap");
            InsaneSkillsPatches.Do(SkillsMod.Harm);
        }

        if (SkillsMod.CharacterEditor) CharacterEditorPatches.Do(SkillsMod.Harm);

        if (SkillsMod.PrepareCarefully) PrepareCarefullyPatches.Do(SkillsMod.Harm);
    }
}