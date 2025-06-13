using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Text;
using System.Reflection;
using VSE.Passions;
using System.Linq;

namespace VSE
{


    public static class VSE_ChoiceLetter_GrowthMoment_IsValidGrowthPassionOption_Patch
    {
        public static IEnumerable<CodeInstruction> BetterSelectUpgradeablePassions(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            var info = AccessTools.Field(typeof(SkillRecord), nameof(SkillRecord.passion));
            var idx1 = codes.FindIndex(ins => ins.LoadsField(info));
            codes.RemoveRange(idx1+1,2);

            codes.InsertRange(idx1+1, new[] {
            CodeInstruction.Call(typeof(VSE_ChoiceLetter_GrowthMoment_IsValidGrowthPassionOption_Patch), nameof(VSE_ChoiceLetter_GrowthMoment_IsValidGrowthPassionOption_Patch.SelectUpgradeablePassions))
            }
        );
            return codes;
        }

        public static bool SelectUpgradeablePassions(Passion passion)
        {
            PassionDef passionDef = PassionManager.PassionToDef(passion);
            return !passionDef.upgradeableInGrowthMoments;

        }

     

    }













}

