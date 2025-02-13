using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
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
    private static Vector2 scrollPosition = Vector2.zero;

    public SkillsMod(ModContentPack content) : base(content)
    {
        Harm = new Harmony("vanillaexpanded.skills");
        Settings = GetSettings<SkillsModSettings>();
        try
        {
            ExpertisePatches.Do(Harm);
            StatPatches.Do(Harm);
            PassionPatches.Do(Harm);
            ModCompat.Init();
        }
        catch (Exception) { }
       
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
        var scrollContainer = inRect.ContractedBy(10);
        scrollContainer.height -= listing.CurHeight;
        scrollContainer.y += listing.CurHeight;
        Widgets.DrawBoxSolid(scrollContainer, Color.grey);
        var innerContainer = scrollContainer.ContractedBy(1);
        Widgets.DrawBoxSolid(innerContainer, new ColorInt(42, 43, 44).ToColor);
        var frameRect = innerContainer.ContractedBy(5);
        frameRect.y += 15;
        frameRect.height -= 15;
        var contentRect = frameRect;
        contentRect.x = 0;
        contentRect.y = 0;
        contentRect.width -= 20;
        int numberPassions = DefDatabase<PassionDef>.AllDefsListForReading.Where(x => !x.isTriggered).Count();

        contentRect.height = numberPassions * 32 + 380f;

        Widgets.BeginScrollView(frameRect, ref scrollPosition, contentRect, true);

        listing.Begin(contentRect.AtZero());
        if (listing.ButtonTextLabeled("VSE.MaxExpertise".Translate(), Settings.MaxExpertise.ToString()))
            Find.WindowStack.Add(new FloatMenu(Enumerable.Range(1, DefDatabase<ExpertiseDef>.DefCount)
               .Select(static num =>
                    new FloatMenuOption(num.ToString(), () => Settings.MaxExpertise = num))
               .ToList()));

        listing.CheckboxLabeled("VSE.EnableAlert".Translate(), ref Settings.EnableAlert);
        listing.CheckboxLabeled("VSE.AllowMultiCritical".Translate(), ref Settings.AllowMultipleCritical, "VSE.AllowMultiCritical.Desc".Translate());
        listing.CheckboxLabeled("VSE.CriticalEffectPassions".Translate(), ref Settings.CriticalEffectPassions, "VSE.CriticalEffectPassions.Desc".Translate());
        listing.CheckboxLabeled("VSE.AllowExpertiseOverlap".Translate(), ref Settings.AllowExpertiseOverlap, "VSE.AllowExpertiseOverlap.Desc".Translate());
        if (ModCompat.InsaneSkills)
            listing.CheckboxLabeled("VSE.EnableSkillLoss".Translate(), ref Settings.EnableSkillLoss, "VSE.EnableSkillsLoss.Desc".Translate());

        listing.LabelPlus("VSE.MinSkillForExpertise".Translate() + ": " + Settings.LevelToGetExpertise, "VSE.MinSkillForExpertiseDesc".Translate());
        Settings.LevelToGetExpertise = (int)Math.Round(listing.Slider(Settings.LevelToGetExpertise, 1f, 20f), 1);

        listing.LabelPlus("VSE.StatMultiplier".Translate() + ": " + Settings.StatMultiplier, "VSE.StatMultiplierDesc".Translate());
        Settings.StatMultiplier = (float)Math.Round(listing.Slider(Settings.StatMultiplier, 0.1f, 5f), 1);

        listing.LabelPlus("VSE.GrowthMomentRandomPassions".Translate() + ": " + Settings.GrowthMomentRandomPassionsChance, "VSE.GrowthMomentRandomPassionsDesc".Translate());
        Settings.GrowthMomentRandomPassionsChance = (float)Math.Round(listing.Slider(Settings.GrowthMomentRandomPassionsChance, 1f, 100f), 0);


        var height = Text.LineHeight * (DefDatabase<PassionDef>.AllDefs.Where(def => !def.isTriggered).Count() + 2) + 50f;
        var inner = listing.BeginSection(height);
        if (inner.ButtonTextLabeled("VSE.Commonalities".Translate(), "VSE.Reset".Translate())) Settings.PassionCommonalities.Clear();

        void DoEdit(PassionDef def)
        {
            var rect = inner.GetRect(Text.LineHeight);
            if (!Settings.PassionCommonalities.TryGetValue(def.defName, out var commonality)) commonality = defaultCommonalities[def.defName];
            Widgets.Label(rect.TakeLeftPart(rect.width * 0.25f), def.LabelCap + ": " + commonality);
            Settings.PassionCommonalities[def.defName] = Widgets.HorizontalSlider(rect, commonality, 0f, 10f);
        }

        inner.Label("VSE.Good".Translate().Colorize(Color.yellow));
        foreach (var def in DefDatabase<PassionDef>.AllDefs.Where(def => !def.isBad && !def.isTriggered)) DoEdit(def);
        inner.Label("VSE.Bad".Translate().Colorize(Color.yellow));
        foreach (var def in DefDatabase<PassionDef>.AllDefs.Where(def => def.isBad && !def.isTriggered)) DoEdit(def);

        listing.EndSection(inner);

        listing.End();
        Widgets.EndScrollView();
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
    public int LevelToGetExpertise = 15;
    public float StatMultiplier = 1;
    public float GrowthMomentRandomPassionsChance = 5;

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
        Scribe_Values.Look(ref LevelToGetExpertise, "LevelToGetExpertise", 15);
        Scribe_Values.Look(ref StatMultiplier, "StatMultiplier", 1);
        Scribe_Values.Look(ref GrowthMomentRandomPassionsChance, "GrowthMomentRandomPassionsChance", 5);


        Scribe_Collections.Look(ref PassionCommonalities, "passionCommonalities", LookMode.Value, LookMode.Value);
        PassionCommonalities ??= new Dictionary<string, float>();
    }
}
