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

        if (skillRecord.Level < SkillsMod.Settings.LevelToGetExpertise)
        {
            reason = "VSE.SkillTooLow".Translate();
            return false;
        }

        if (pawn.Expertise().AllExpertise.Count >= SkillsMod.Settings.MaxExpertise)
        {
            reason = "VSE.AtMaxExpertise".Translate();
            return false;
        }

        if (!SkillsMod.Settings.AllowExpertiseOverlap && pawn.Expertise().AllExpertise.Any(e => e.def.skill == skill))
        {
            reason = "VSE.ExpertiseOverlap".Translate();
            return false;
        }

        reason = null;
        return true;
    }

    public string Effects(int level = 0, string prefix = "") =>
        statOffsets.Aggregate(
            statFactors.Aggregate("",
                (current, factor) =>
                    current +
                    $"\n{prefix}{factor.stat.LabelCap}: {factor.stat.Worker.ValueToString(1 + factor.value * level, false, ToStringNumberSense.Factor)}"),
            (current, offset) =>
                current +
                $"\n{prefix}{offset.stat.LabelCap}: {offset.stat.Worker.ValueToString(offset.value * level, false, ToStringNumberSense.Offset)}");
}