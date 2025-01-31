using RimWorld;
using UnityEngine;
using Verse;
using System.Collections.Generic;

// ReSharper disable InconsistentNaming

namespace VSE.Passions;

public class PassionDef : Def
{
    public PassionColor color = PassionColor.Main;
    public float commonality = 1f;
    public float forgetRateFactor = 1f;
    public string iconPath;

    [NoTranslate] public string indicatorString;

    public float inspirationCommonality;
    public bool isBad = false;
    public float learnRateFactor = 1f;
    public float learnRateFactorOther = 1f;
    public string workBoxIconPath;
    private Texture2D icon;
    private Texture2D workBoxIcon;
    public string Indicator => indicatorString.NullOrEmpty() ? "" : " " + indicatorString;
    public Texture2D Icon => icon ??= ContentFinder<Texture2D>.Get(iconPath);
    public Texture2D WorkBoxIcon => workBoxIcon ??= ContentFinder<Texture2D>.Get(workBoxIconPath);
    public List<TraitDef> blockingTraits = new List<TraitDef>();
    public List<PreceptDef> blockingPrecepts = new List<PreceptDef>();
    public List<GeneDef> blockingGenes = new List<GeneDef>();

    public string FullDescription =>
        LabelCap + "VSE.LearnsForgets".Translate(learnRateFactor.ToStringPercent(), forgetRateFactor.ToStringPercent()) +
        (Mathf.Approximately(learnRateFactorOther, 1f)
            ? ""
            : "VSE.LearnOther".Translate(learnRateFactorOther.ToStringPercent()).Resolve());

    public bool IsCritical => !Mathf.Approximately(learnRateFactorOther, 1f);
}

public enum PassionColor
{
    Disabled,
    Main,
    Minor,
    Major
}

[DefOf]
public static class PassionDefOf
{
    public static PassionDef None;
    public static PassionDef Minor;
    public static PassionDef Major;

    static PassionDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(PassionDefOf));
    }
}
