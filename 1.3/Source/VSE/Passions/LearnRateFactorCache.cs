using System.Linq;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;

namespace VSE.Passions;

public static class LearnRateFactorCache
{
    private static ConditionalWeakTable<SkillRecord, CacheData> cache = new();

    public static float LearnRateFactorBase(this SkillRecord sr) => cache.GetValue(sr, sr => new CacheData { Value = GetValueFor(sr) }).Value;

    public static void ClearCacheFor(SkillRecord sr)
    {
        cache.Remove(sr);
    }

    public static void ClearCache() => cache = new ConditionalWeakTable<SkillRecord, CacheData>();

    private static float GetValueFor(SkillRecord sr)
    {
        var passionDef = PassionManager.PassionToDef(sr.passion);
        if (SkillsMod.Settings.CriticalEffectPassions || passionDef.isBad)
            return sr.pawn.skills.skills
                .Except(sr)
                .Aggregate(passionDef.learnRateFactor,
                    (current, skillRecord) => current * PassionManager.PassionToDef(skillRecord.passion).learnRateFactorOther);
        return passionDef.learnRateFactor;
    }

    private class CacheData
    {
        public float Value;
    }
}