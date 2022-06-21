using System.Linq;
using RimWorld;
using Verse;

namespace VSE.Passions
{
    [StaticConstructorOnStartup]
    public static class PassionManager
    {
        public static PassionDef[] Passions;

        static PassionManager()
        {
            foreach (var passionDef in DefDatabase<PassionDef>.AllDefs) passionDef.index = ushort.MaxValue;
            Passions = new PassionDef[DefDatabase<PassionDef>.DefCount];
            ushort i = 0;
            PassionDefOf.None.index = i++;
            PassionDefOf.Minor.index = i++;
            PassionDefOf.Major.index = i++;
            foreach (var passionDef in DefDatabase<PassionDef>.AllDefs.Where(def => def.index == ushort.MaxValue)) passionDef.index = i++;
            foreach (var passionDef in DefDatabase<PassionDef>.AllDefs) Passions[passionDef.index] = passionDef;
            if (Passions.Length >= byte.MaxValue) Log.Error("[Vanilla Skills Expanded] Too many PassionDefs, this will cause issues");
        }

        public static PassionDef PassionToDef(Passion passion) => Passions[(ushort) passion];

        public static float ForgetRateFactor(this SkillRecord skillRecord) => PassionToDef(skillRecord.passion).forgetRateFactor;
    }
}