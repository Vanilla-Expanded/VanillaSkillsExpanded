using RimWorld;
using UnityEngine;
using Verse;

// ReSharper disable InconsistentNaming

namespace VSE.Passions;

public class PassionDef : Def
{
    public float commonality = 1f;
    public float forgetRateFactor = 1f;
    private Texture2D icon;
    public string iconPath;
    public float inspirationCommonality;
    public bool isBad = false;
    public float learnRateFactor = 1f;
    public float learnRateFactorOther = 1f;
    public Texture2D Icon => icon ??= ContentFinder<Texture2D>.Get(iconPath);

    public string FullDescription => LabelCap + "VSE.LearnsForgets".Translate(learnRateFactor.ToStringPercent(), forgetRateFactor.ToStringPercent()) +
                                     (Mathf.Approximately(learnRateFactorOther, 1f)
                                         ? ""
                                         : "VSE.LearnOther".Translate(learnRateFactorOther.ToStringPercent()).Resolve());

    public bool IsCritical => !Mathf.Approximately(learnRateFactorOther, 1f);
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