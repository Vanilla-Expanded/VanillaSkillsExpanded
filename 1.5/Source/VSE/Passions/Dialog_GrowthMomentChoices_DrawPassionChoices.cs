using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Text;
using System.Reflection;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using VSE.Passions;

namespace VSE
{


    public static class VSE_Dialog_GrowthMomentChoices_DrawPassionChoices_Patch
    {


       
        public static IEnumerable<CodeInstruction> TweakPassionDrawings(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            var info = AccessTools.Field(typeof(SkillUI), nameof(SkillUI.PassionMinorIcon));
            var idx1 = codes.FindIndex(ins => ins.LoadsField(info));
            
            codes.RemoveRange(idx1-2, 5);
            codes.InsertRange(idx1-2, new[]
            {
            CodeInstruction.Call(typeof(VSE_Dialog_GrowthMomentChoices_DrawPassionChoices_Patch), nameof(VSE_Dialog_GrowthMomentChoices_DrawPassionChoices_Patch.ChoosePassionImage)),
            });

            var info2 = AccessTools.Method(typeof(PassionExtension), nameof(PassionExtension.IncrementPassion));
            var idx2 = codes.FindIndex(ins => ins.Calls(info2));
            codes.RemoveAt(idx2);

            codes.InsertRange(idx2, new[] {
            CodeInstruction.Call(typeof(VSE_ChoiceLetter_GrowthMoment_MakeChoices_Patch), nameof(VSE_ChoiceLetter_GrowthMoment_MakeChoices_Patch.ChooseIncrementedPassion))
            }
            );



            return codes;
        }

        public static Texture2D ChoosePassionImage(Passion passion)
        {
            return PassionManager.PassionToDef(passion).Icon;
        }


    }













}

