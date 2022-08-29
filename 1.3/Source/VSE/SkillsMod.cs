using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Verse;
using VFECore.UItils;
using VSE.Expertise;
using VSE.Passions;
using VSE.Stats;

namespace VSE;

public class SkillsMod : Mod
{
    public static SkillsModSettings Settings;
    public static Harmony Harm;
    private static Dictionary<string, float> defaultCommonalities;

    public SkillsMod(ModContentPack content) : base(content)
    {
        Harm = new Harmony("vanillaexpanded.skills");
        Settings = GetSettings<SkillsModSettings>();
        ExpertisePatches.Do(Harm);
        StatPatches.Do(Harm);
        PassionPatches.Do(Harm);
        ModCompat.Init();
        LongEventHandler.ExecuteWhenFinished(delegate
        {
            defaultCommonalities = DefDatabase<PassionDef>.AllDefs.ToDictionary(def => def.defName, def => def.commonality);
            ApplySettings();
        });
    }

    public override string SettingsCategory() => "VSE".Translate();

    public override void DoSettingsWindowContents(Rect inRect)
    {
        base.DoSettingsWindowContents(inRect);
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        if (listing.ButtonTextLabeled("VSE.MaxExpertise".Translate(), Settings.MaxExpertise.ToString()))
        {
            var list = new List<FloatMenuOption>();
            for (var i = 1; i < 6; i++)
            {
                var num = i;
                list.Add(new FloatMenuOption(i.ToString(), () => Settings.MaxExpertise = num));
            }

            Find.WindowStack.Add(new FloatMenu(list));
        }

        listing.CheckboxLabeled("VVE.AllowMultiCritical".Translate(), ref Settings.AllowMultipleCritical, "VVE.AllowMultiCritical.Desc".Translate());
        listing.CheckboxLabeled("VVE.CriticalEffectPassions".Translate(), ref Settings.CriticalEffectPassions, "VVE.CriticalEffectPassions.Desc".Translate());
        if (ModCompat.InsaneSkills)
            listing.CheckboxLabeled("VSE.EnableSkillLoss".Translate(), ref Settings.EnableSkillLoss, "VSE.EnableSkillsLoss.Desc".Translate());
        var height = Text.LineHeight * (DefDatabase<PassionDef>.DefCount + 2) + 50f;
        var inner = listing.BeginSection(height);
        if (inner.ButtonTextLabeled("VVE.Commonalities".Translate(), "VVE.Reset".Translate())) Settings.PassionCommonalities.Clear();

        void DoEdit(PassionDef def)
        {
            var rect = inner.GetRect(Text.LineHeight);
            var commonality = Settings.PassionCommonalities.GetValueOrDefault(def.defName, defaultCommonalities[def.defName]);
            Widgets.Label(rect.TakeLeftPart(rect.width * 0.25f), def.LabelCap + ": " + commonality);
            Settings.PassionCommonalities[def.defName] = Widgets.HorizontalSlider(rect, commonality, 0f, 10f);
        }

        inner.Label("VVE.Good".Translate());
        foreach (var def in DefDatabase<PassionDef>.AllDefs.Where(def => !def.isBad)) DoEdit(def);
        inner.Label("VVE.Bad".Translate());
        foreach (var def in DefDatabase<PassionDef>.AllDefs.Where(def => def.isBad)) DoEdit(def);

        listing.EndSection(inner);

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
        if (Current.ProgramState == ProgramState.Playing) LearnRateFactorCache.ClearCache();
        foreach (var def in DefDatabase<PassionDef>.AllDefs)
            if (Settings.PassionCommonalities.TryGetValue(def.defName, out var val)) def.commonality = val;
            else if (defaultCommonalities.TryGetValue(def.defName, out val)) def.commonality = val;
    }
}

public class SkillsModSettings : ModSettings
{
    public bool AllowMultipleCritical = true;
    public bool CriticalEffectPassions = true;
    public bool EnableSkillLoss;
    public int MaxExpertise = 1;
    public Dictionary<string, float> PassionCommonalities = new();

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref EnableSkillLoss, "enableSkillsLoss");
        Scribe_Values.Look(ref MaxExpertise, "maxExpertise", 1);
        Scribe_Values.Look(ref AllowMultipleCritical, "allowMultipleCritical", true);
        Scribe_Values.Look(ref CriticalEffectPassions, "criticalEffectPassions", true);
        Scribe_Collections.Look(ref PassionCommonalities, "passionCommonalities", LookMode.Value, LookMode.Value);
        PassionCommonalities ??= new Dictionary<string, float>();
    }
}