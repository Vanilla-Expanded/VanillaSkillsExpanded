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
            for (var i = 1; i < 11; i++)
            {
                var num = i;
                list.Add(new FloatMenuOption(i.ToString(), () => Settings.MaxExpertise = num));
            }

            Find.WindowStack.Add(new FloatMenu(list));
        }

        listing.CheckboxLabeled("VSE.EnableAlert".Translate(), ref Settings.EnableAlert);
        listing.CheckboxLabeled("VSE.AllowMultiCritical".Translate(), ref Settings.AllowMultipleCritical, "VSE.AllowMultiCritical.Desc".Translate());
        listing.CheckboxLabeled("VSE.CriticalEffectPassions".Translate(), ref Settings.CriticalEffectPassions, "VSE.CriticalEffectPassions.Desc".Translate());
        listing.CheckboxLabeled("VSE.AllowExpertiseOverlap".Translate(), ref Settings.AllowExpertiseOverlap, "VSE.AllowExpertiseOverlap.Desc".Translate());
        if (ModCompat.InsaneSkills)
            listing.CheckboxLabeled("VSE.EnableSkillLoss".Translate(), ref Settings.EnableSkillLoss, "VSE.EnableSkillsLoss.Desc".Translate());
        var height = Text.LineHeight * (DefDatabase<PassionDef>.DefCount + 2) + 50f;
        var inner = listing.BeginSection(height);
        if (inner.ButtonTextLabeled("VSE.Commonalities".Translate(), "VSE.Reset".Translate())) Settings.PassionCommonalities.Clear();

        void DoEdit(PassionDef def)
        {
            var rect = inner.GetRect(Text.LineHeight);
            if (!Settings.PassionCommonalities.TryGetValue(def.defName, out var commonality)) commonality = defaultCommonalities[def.defName];
            Widgets.Label(rect.TakeLeftPart(rect.width * 0.25f), def.LabelCap + ": " + commonality);
            Settings.PassionCommonalities[def.defName] = Widgets.HorizontalSlider(rect, commonality, 0f, 10f);
        }

        inner.Label("VSE.Good".Translate());
        foreach (var def in DefDatabase<PassionDef>.AllDefs.Where(def => !def.isBad)) DoEdit(def);
        inner.Label("VSE.Bad".Translate());
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
        float total = 0;
        foreach (var def in DefDatabase<PassionDef>.AllDefs)
            if (Settings.PassionCommonalities.TryGetValue(def.defName, out var val)) total += def.commonality = val;
            else if (defaultCommonalities.TryGetValue(def.defName, out val)) total += def.commonality = val;
        if (total == 0)
        {
            Log.Warning("[VSE] Total commonality is 0. This will cause errors. Setting None to 1.");
            PassionDefOf.None.commonality = 1f;
        }
    }
}

public class SkillsModSettings : ModSettings
{
    public bool AllowExpertiseOverlap = true;
    public bool AllowMultipleCritical = true;
    public bool CriticalEffectPassions = true;
    public bool EnableAlert = true;
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
        Scribe_Values.Look(ref EnableAlert, "enableAlert", true);
        Scribe_Values.Look(ref AllowExpertiseOverlap, "allowExpertiseOverlap", true);
        Scribe_Collections.Look(ref PassionCommonalities, "passionCommonalities", LookMode.Value, LookMode.Value);
        PassionCommonalities ??= new Dictionary<string, float>();
    }
}