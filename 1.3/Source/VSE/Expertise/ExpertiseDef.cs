using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using VSE.Passions;

namespace VSE.Expertise;

public class ExpertiseDef : Def
{
    public RulePackDef flavorMaker;
    public SkillDef skill;
    public List<StatModifier> statFactors = new();
    public List<StatModifier> statOffsets = new();

    public bool CanApplyOn(Pawn pawn, out string reason)
    {
        if (pawn.skills?.GetSkill(skill) is not { } skillRecord)
        {
            reason = null;
            return false;
        }

        if (PassionManager.PassionToDef(skillRecord.passion).isBad)
        {
            reason = "VSE.NoPassion".Translate();
            return false;
        }

        if (skillRecord.Level < 15)
        {
            reason = "VSE.SkillTooLow".Translate();
            return false;
        }

        if (pawn.Expertise().AllExpertise.Count() >= SkillsMod.Settings.MaxExpertise)
        {
            reason = "VSE.AtMaxExpertise".Translate();
            return false;
        }

        reason = null;
        return true;
    }

    public string FullDescription(int level = 0) =>
        statOffsets.Aggregate(
            statFactors.Aggregate(description,
                (current, factor) =>
                    current +
                    $"\n{factor.stat.LabelCap}: {factor.stat.Worker.ValueToString(factor.value * (level + 1), false, ToStringNumberSense.Factor)}"),
            (current, offset) =>
                current + $"\n{offset.stat.LabelCap}: {offset.stat.Worker.ValueToString(offset.value * (level + 1), false, ToStringNumberSense.Offset)}");
}