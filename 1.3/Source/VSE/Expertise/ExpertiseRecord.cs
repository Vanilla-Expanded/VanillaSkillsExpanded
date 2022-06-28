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

        public float XpProgressPercent => XpSinceLastLevel / XpRequiredForLevelUp;

        public float XpSinceLastLevel { get; private set; }

        public float XpRequiredForLevelUp => SkillRecord.XpRequiredToLevelUpFrom(level);

        public Pawn Pawn { get; }

        public int Level => level;
        public int LevelPlusOne => level + 1;

        public string LevelDescriptor
        {
            get
            {
                if (level < 0 || level > 20) return "Unknown".Translate();
                return $"VSE.Expertise{level}".Translate();
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
            XpSinceLastLevel += xp;
            while (XpSinceLastLevel >= XpRequiredForLevelUp)
            {
                XpSinceLastLevel -= XpRequiredForLevelUp;
                level++;
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
}