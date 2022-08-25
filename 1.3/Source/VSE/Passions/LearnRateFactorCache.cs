using System.Linq;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;

namespace VSE.Passions;

public static class LearnRateFactorCache
{
    private static readonly ConditionalWeakTable<SkillRecord, CacheData> cache = new();

    public static float LearnRateFactorBase(this SkillRecord sr) => cache.GetValue(sr, sr => new CacheData { Value = GetValueFor(sr) }).Value;

    public static void ClearCacheFor(SkillRecord sr)
    {
        cache.Remove(sr);
    }

    private static float GetValueFor(SkillRecord sr) => sr.pawn.skills.skills
        .Except(sr)
        .Aggregate(PassionManager.PassionToDef(sr.passion).learnRateFactor,
            (current, skillRecord) => current * PassionManager.PassionToDef(skillRecord.passion).learnRateFactorOther);

    private class CacheData
    {
        public float Value;
    }
}