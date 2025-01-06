using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;
using VSE.Expertise;

namespace VSE;

public class ExpertiseTracker : IExposable
{
    private readonly Pawn_SkillTracker skills;
    private List<ExpertiseRecord> expertise = new();

    public ExpertiseTracker(Pawn_SkillTracker parent) => skills = parent;
    public List<ExpertiseRecord> AllExpertise => expertise;

    public Pawn Pawn => skills.pawn;

    public void ExposeData()
    {
        Scribe_Collections.Look(ref expertise, "expertise", LookMode.Deep, Pawn);
        if (Scribe.mode == LoadSaveMode.PostLoadInit && expertise == null) expertise = new List<ExpertiseRecord>();
    }

    public void AddExpertise(ExpertiseDef expertiseDef)
    {
        expertise.Add(new ExpertiseRecord(Pawn, expertiseDef));
    }

    public void ClearExpertise()
    {
        expertise.Clear();
    }

    public bool HasExpertise() => expertise.Count > 0;

    public void OffsetStat(StatDef stat, ref float value)
    {
        for (var i = 0; i < expertise.Count; i++)
        {
            var expertiseRecord = expertise[i];
            value += expertiseRecord.def.statOffsets.GetStatOffsetFromList(stat) * expertiseRecord.Level * SkillsMod.Settings.StatMultiplier;
        }
    }

    public void OffsetStatExplain(StatDef stat, StringBuilder builder)
    {
        var index = builder.Length;
        var addedAnything = false;
        for (var i = 0; i < expertise.Count; i++)
        {
            var expertiseRecord = expertise[i];
            var offset = expertiseRecord.def.statOffsets.GetStatOffsetFromList(stat);
            if (offset != 0f)
            {
                builder.AppendLine(
                    $"  {expertiseRecord.def.LabelCap} ({expertiseRecord.Level}): {stat.Worker.ValueToString(offset * expertiseRecord.Level * SkillsMod.Settings.StatMultiplier, false, ToStringNumberSense.Offset)}");
                addedAnything = true;
            }
        }

        if (addedAnything) builder.Insert(index, "Expertise:\n");
    }

    public void MultiplyStatExplain(StatDef stat, StringBuilder builder)
    {
        var index = builder.Length;
        var addedAnything = false;
        for (var i = 0; i < expertise.Count; i++)
        {
            var expertiseRecord = expertise[i];
            var factorOffset = expertiseRecord.def.statFactors.GetStatValueFromList(stat, 0f);
            if (factorOffset != 0f)
            {
                builder.AppendLine(
                    $"  {expertiseRecord.def.LabelCap} ({expertiseRecord.Level}): {stat.Worker.ValueToString(1f + factorOffset * expertiseRecord.Level * SkillsMod.Settings.StatMultiplier, false, ToStringNumberSense.Factor)}");
                addedAnything = true;
            }
        }

        if (addedAnything) builder.Insert(index, "Expertise:\n");
    }

    public void MultiplyStat(StatDef stat, ref float value)
    {
        for (var i = 0; i < expertise.Count; i++)
        {
            var expertiseRecord = expertise[i];
            var factor = expertiseRecord.def.statFactors.GetStatValueFromList(stat, 0f);
            if (factor != 0f) value *= 1f + factor * expertiseRecord.Level * SkillsMod.Settings.StatMultiplier;
        }
    }
}