using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Grammar;
using VSE.Expertise;

namespace VSE;

public class ExpertiseRecord : IExposable
{
    public ExpertiseDef def;
    private int level;

    public float XpRequiredForLevelUp;

    public float XpSinceLastLevel;

    public ExpertiseRecord()
    {
    }

    public ExpertiseRecord(Pawn pawn) => Pawn = pawn;

    public ExpertiseRecord(Pawn pawn, ExpertiseDef def)
    {
        Pawn = pawn;
        this.def = def;
        this.XpRequiredForLevelUp = SkillRecord.XpRequiredToLevelUpFrom(0);
    }

    public float XpTotalEarned
    {
        get
        {
            var num = 0f;
            for (var i = 0; i < level; i++) num += SkillRecord.XpRequiredToLevelUpFrom(i);
            return num;
        }
    }

    public float XpProgressPercent => XpSinceLastLevel / XpRequiredForLevelUp;

    public Pawn Pawn { get; }

    public int Level
    {
        get { return level; }
        set { level = value; }

    } 

    public string LevelDescriptor => level is < 0 or > 20 ? "Unknown".Translate() : $"VSE.Expertise{level}".Translate();

    public void ExposeData()
    {
        Scribe_Defs.Look(ref def, "def");
        Scribe_Values.Look(ref level, "level");
        Scribe_Values.Look(ref XpSinceLastLevel, "xp");
        XpRequiredForLevelUp = SkillRecord.XpRequiredToLevelUpFrom(level);
    }

    public string FullDescription()
    {
        var builder = new StringBuilder();
        builder.AppendLineTagged(def.LabelCap.AsTipTitle());
        builder.AppendLineTagged(def.description.Colorize(ColoredText.SubtleGrayColor));
        builder.AppendLine();
        builder.AppendLineTagged($"{("Level".Translate().CapitalizeFirst() + ": ").AsTipTitle()}{Level} - {LevelDescriptor}");
        builder.AppendLine();
        builder.AppendLineTagged(
            $"{(Level == 20 ? "Experience".Translate() : "ProgressToNextLevel".Translate()).AsTipTitle()}{": ".AsTipTitle()}{XpSinceLastLevel:F0} / {XpRequiredForLevelUp}");
        builder.AppendLine();
        builder.AppendLineTagged("VSE.Effects".Translate().AsTipTitle());
        builder.AppendLine(def.Effects(Level, "  - "));
        if (def.flavorMaker is not null)
            builder.AppendLine().AppendLine().AppendLine(GrammarResolver.Resolve("root", new GrammarRequest
            {
                Includes =
                {
                    def.flavorMaker
                }
            }));

        return builder.ToString();
    }

    public void Learn(float xp)
    {
        XpSinceLastLevel += xp;
        while (XpSinceLastLevel >= XpRequiredForLevelUp)
        {
            XpSinceLastLevel -= XpRequiredForLevelUp;
            level++;
            foreach (StatDef item in DefDatabase<StatDef>.AllDefsListForReading)
            {
                item.Worker.TryClearCache();
            }
            XpRequiredForLevelUp = SkillRecord.XpRequiredToLevelUpFrom(level);
            if (level >= 20)
            {
                level = 20;
                XpSinceLastLevel = Mathf.Clamp(XpSinceLastLevel, 0f, XpRequiredForLevelUp - 1f);
                while (XpSinceLastLevel <= -1000f)
                {
                    level--;
                    XpSinceLastLevel += XpRequiredForLevelUp;
                    if (level <= 0)
                    {
                        level = 0;
                        XpSinceLastLevel = 0f;
                        break;
                    }
                }

                return;
            }
        }
    }
}