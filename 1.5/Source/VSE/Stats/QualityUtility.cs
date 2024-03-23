using RimWorld;
using Verse;

namespace VSE.Stats
{
    public static class QualityUtility
    {
        public static QualityCategory GenerateQuality(Pawn worker, SkillDef workSkill, Thing thing = null)
        {
            var start = RimWorld.QualityUtility.GenerateQualityCreatedByPawn(worker, workSkill);
            if (workSkill == SkillDefOf.Artistic) start.AddFromStat(worker.GetStatValue(MoreStatDefOf.VSE_ArtQuality));
            if (workSkill == SkillDefOf.Construction) start.AddFromStat(worker.GetStatValue(MoreStatDefOf.VSE_ConstructQuality));
            return start;
        }

        public static void AddFromStat(ref this QualityCategory initial, float statValue)
        {
            while (statValue >= 0f)
            {
                if (Rand.Chance(statValue)) initial = RimWorld.QualityUtility.AddLevels(initial, 1);

                statValue -= 1f;
            }
        }
    }
}