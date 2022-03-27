using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using VSE.Expertise;

namespace VSE
{
    public class SkillsMod : Mod
    {
        private static Dictionary<SkillDef, List<ExpertiseDef>> expertiseForSkill;

        public SkillsMod(ModContentPack content) : base(content)
        {
            var harm = new Harmony("vanillaexpanded.skills");
            HarmonyPatches.Do(harm);
            Stats.HarmonyPatches.Do(harm);
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                expertiseForSkill = new Dictionary<SkillDef, List<ExpertiseDef>>();
                foreach (var skill in DefDatabase<SkillDef>.AllDefs) expertiseForSkill.Add(skill, new List<ExpertiseDef>());
                foreach (var expertiseDef in DefDatabase<ExpertiseDef>.AllDefs) expertiseForSkill[expertiseDef.skill].Add(expertiseDef);
            });
        }

        public static IEnumerable<ExpertiseDef> AllExpertiseForSkill(SkillDef skill) => expertiseForSkill[skill];
    }
}