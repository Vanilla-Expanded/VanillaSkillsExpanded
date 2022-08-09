using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using VSE.Expertise;
using VSE.Passions;
using VSE.Stats;

namespace VSE;

public class SkillsMod : Mod
{
    public static bool InsaneSkills;
    public static bool CharacterEditor;
    public static bool PrepareCarefully;
    public static SkillsModSettings Settings;
    public static Harmony Harm;

    public SkillsMod(ModContentPack content) : base(content)
    {
        Harm = new Harmony("vanillaexpanded.skills");
        InsaneSkills = ModLister.HasActiveModWithName("Ducks' Insane Skills");
        CharacterEditor = ModLister.HasActiveModWithName("Character Editor");
        PrepareCarefully = ModLister.HasActiveModWithName("EdB Prepare Carefully");
        Settings = GetSettings<SkillsModSettings>();
        ExpertisePatches.Do(Harm);
        StatPatches.Do(Harm);
        PassionPatches.Do(Harm);
        ApplySettings();
        ModCompat.Init();
    }

    public override string SettingsCategory() => "VSE".Translate();

    public override void DoSettingsWindowContents(Rect inRect)
    {
        base.DoSettingsWindowContents(inRect);
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        if (InsaneSkills) listing.CheckboxLabeled("VSE.EnableSkillLoss".Translate(), ref Settings.EnableSkillLoss, "VSE.EnableSkillsLoss.Desc".Translate());
        listing.End();
    }

    public override void WriteSettings()
    {
        base.WriteSettings();
        ApplySettings();
    }

    private static void ApplySettings()
    {
        if (InsaneSkills && Settings.EnableSkillLoss)
        {
            var type = AccessTools.TypeByName("DucksInsaneSkills.DucksSkills_Interval");
            Harm.Unpatch(AccessTools.Method(typeof(SkillRecord), "Interval"), AccessTools.Method(type, "Prefix"));
        }
    }
}

public class SkillsModSettings : ModSettings
{
    public bool EnableSkillLoss;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref EnableSkillLoss, "enableSkillsLoss");
    }
}