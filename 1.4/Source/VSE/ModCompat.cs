using System;
using System.Reflection;
using HarmonyLib;
using MonoMod.Utils;
using RimWorld;
using Verse;

namespace VSE;

public static class ModCompat
{
    private static Func<float> saturatedXPMultiplier;
    private static Action<object, Passion> setPassion;
    private static Func<object, Passion> getPassion;
    public static MethodInfo SD_passion;

    public static bool InsaneSkills;
    public static bool CharacterEditor;
    public static bool PrepareCarefully;
    public static bool MadSkills;
    public static bool PrepareModerately;
    public static bool RandomPlus;
    public static bool StrangerDanger;

    public static float SaturatedXPMultiplier => saturatedXPMultiplier();

    public static Passion GetPassion(object obj) => getPassion(obj);
    public static void SetPassion(object obj, Passion passion) => setPassion(obj, passion);

    public static void Init()
    {
        InsaneSkills = ModLister.HasActiveModWithName("Ducks' Insane Skills");
        CharacterEditor = ModLister.HasActiveModWithName("Character Editor");
        PrepareCarefully = ModLister.HasActiveModWithName("EdB Prepare Carefully");
        MadSkills = ModLister.HasActiveModWithName("Mad Skills");
        PrepareModerately = ModLister.HasActiveModWithName("Prepare Moderately");
        RandomPlus = ModLister.HasActiveModWithName("RandomPlus");
        StrangerDanger = ModLister.HasActiveModWithName("Stranger Danger");

        if (InsaneSkills) InsaneSkillsPatches.Do(SkillsMod.Harm);

        if (CharacterEditor) CharacterEditorPatches.Do(SkillsMod.Harm);

        if (PrepareCarefully) PrepareCarefullyPatches.Do(SkillsMod.Harm);

        if (MadSkills)
            saturatedXPMultiplier = AccessTools.PropertyGetter(AccessTools.TypeByName("RTMadSkills.ModSettings"), "saturatedXPMultiplier")
                .CreateDelegate<Func<float>>();

        if (PrepareModerately) PrepareModeratelyPatches.Do(SkillsMod.Harm);

        if (RandomPlus)
        {
            RandomPlusPatches.Do(SkillsMod.Harm);
            var type = AccessTools.TypeByName("RandomPlus.SkillContainer");
            setPassion = AccessTools.PropertySetter(type, "Passion").CreateDelegate<Action<object, Passion>>();
            getPassion = AccessTools.PropertyGetter(type, "Passion").CreateDelegate<Func<object, Passion>>();
        }

        if (StrangerDanger) SD_passion = AccessTools.TypeByName("Stranger_Danger.ReplacementMethods").GetMethod("SD_passion");
    }
}