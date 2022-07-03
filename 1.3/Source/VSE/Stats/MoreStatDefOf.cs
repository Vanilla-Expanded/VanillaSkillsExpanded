using RimWorld;

namespace VSE.Stats
{
    [DefOf]
    public static class MoreStatDefOf
    {
        public static StatDef VSE_ArmorPenetrationFactor;
        public static StatDef VSE_MeleeCooldownFactor;
        public static StatDef VSE_AttackOnFailChanceFactor;
        public static StatDef VSE_ArtQuality;
        public static StatDef VSE_RoofSpeed;
        public static StatDef VSE_RepairSpeed;
        public static StatDef VSE_ConstructQuality;
        public static StatDef VSE_RockChunkChance;
        public static StatDef VSE_RecruitRate;
        public static StatDef VSE_PeaceTalksChance;
        public static StatDef VSE_FloorSpeed;

        static MoreStatDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(MoreStatDefOf));
        }
    }
}