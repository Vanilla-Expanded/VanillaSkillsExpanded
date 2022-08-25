using HarmonyLib;
using UnityEngine;
using Verse;
using VSE.Expertise;
using VSE.Passions;
using VSE.Stats;

namespace VSE;

public class SkillsMod : Mod
{
    public static SkillsModSettings Settings;
    public static Harmony Harm;

    public SkillsMod(ModContentPack content) : base(content)
    {
        Harm = new Harmony("vanillaexpanded.skills");
        Settings = GetSettings<SkillsModSettings>();
        ExpertisePatches.Do(Harm);
        StatPatches.Do(Harm);
        PassionPatches.Do(Harm);
        ModCompat.Init();
        ApplySettings();
    }

    public override string SettingsCategory() => "VSE".Translate();

    public override void DoSettingsWindowContents(Rect inRect)
    {
        base.DoSettingsWindowContents(inRect);
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        if (ModCompat.InsaneSkills)
            listing.CheckboxLabeled("VSE.EnableSkillLoss".Translate(), ref Settings.EnableSkillLoss, "VSE.EnableSkillsLoss.Desc".Translate());
        listing.End();
    }

    public override void WriteSettings()
    {
        base.WriteSettings();
        ApplySettings();
    }

    private static void ApplySettings()
    {
        if (ModCompat.InsaneSkills) InsaneSkillsPatches.UpdateSkillLoss(Settings.EnableSkillLoss, Harm);
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