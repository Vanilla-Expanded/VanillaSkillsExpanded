using RimWorld;
using UnityEngine;
using Verse;
using Verse.Grammar;
using VSE.Expertise;

namespace VSE
{
    public class ExpertiseRecord : IExposable
    {
        public ExpertiseDef def;
        private int level;
        private float xpSinceLastLevel;

        public ExpertiseRecord()
        {
        }

        public ExpertiseRecord(Pawn pawn) => Pawn = pawn;

        public ExpertiseRecord(Pawn pawn, ExpertiseDef def)
        {
            Pawn = pawn;
            this.def = def;
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

        public float XpProgressPercent => xpSinceLastLevel / XpRequiredForLevelUp;

        public float XpRequiredForLevelUp => SkillRecord.XpRequiredToLevelUpFrom(level);

        public Pawn Pawn { get; }

        public int Level => level;
        public int LevelPlusOne => level + 1;

        public string LevelDescriptor
        {
            get
            {
                if (level < 0 || level > 20) return "Unknown".Translate();
                return $"Skill{level}".Translate();
            }
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref def, "def");
            Scribe_Values.Look(ref level, "level");
        }

        public string FullDescription()
        {
            var text = def.LabelCap + ": " + LevelDescriptor;
            text += "\n\n";
            text += def.FullDescription(Level);
            if (def.flavorMaker is not null)
            {
                text += "\n\n";
                text += GrammarResolver.Resolve("root", new GrammarRequest
                {
                    Includes =
                    {
                        def.flavorMaker
                    }
                });
            }

            return text;
        }

        public void Learn(float xp)
        {
            xpSinceLastLevel += xp;
            while (xpSinceLastLevel >= XpRequiredForLevelUp)
            {
                xpSinceLastLevel -= XpRequiredForLevelUp;
                level++;
                if (level >= 20)
                {
                    level = 20;
                    xpSinceLastLevel = Mathf.Clamp(xpSinceLastLevel, 0f, XpRequiredForLevelUp - 1f);
                    while (xpSinceLastLevel <= -1000f)
                    {
                        level--;
                        xpSinceLastLevel += XpRequiredForLevelUp;
                        if (level <= 0)
                        {
                            level = 0;
                            xpSinceLastLevel = 0f;
                            break;
                        }
                    }

                    return;
                }
            }
        }
    }
}