using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace VSE.Expertise;

public class Alert_ExpertiseAvailable : Alert
{
    private IEnumerable<Pawn> ExpertiseAvailable => PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists.Where(p =>
        DefDatabase<ExpertiseDef>.AllDefs.Except(p.Expertise().AllExpertise.Select(er => er.def)).Any(e => e.CanApplyOn(p, out _)));

    public override AlertReport GetReport() => SkillsMod.Settings.EnableAlert ? AlertReport.CulpritsAre(ExpertiseAvailable.ToList()) : AlertReport.Inactive;

    public override string GetLabel()
    {
        return ExpertiseAvailable.Count() switch
        {
            1 => "VSE.ExpertiseAvailable".Translate(),
            >= 2 => "VSE.ExpertiseAvailableMultiple".Translate(),
            _ => null
        };
    }

    public override TaggedString GetExplanation()
    {
        return "VSE.ExpertiseAvailableDesc".Translate() + ExpertiseAvailable.Select(p => p.NameFullColored.Resolve()).ToLineList("  ");
    }
}