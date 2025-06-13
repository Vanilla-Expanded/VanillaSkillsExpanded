using RimWorld;
using Verse;

namespace VSE.Stats
{
    public static class StatUtility
    {
        public static StatDef ConstructionStatForFrame(this Frame frame) =>
            frame.def.entityDefToBuild is TerrainDef ? MoreStatDefOf.VSE_FloorSpeed : StatDefOf.ConstructionSpeed;
    }
}