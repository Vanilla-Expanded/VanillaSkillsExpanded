using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Text;
using System.Reflection;

namespace VSE
{

  
    public static class VSE_SkillUI_GetSkillDescription_Patch
    {

      
        public static void AddPassionDescription(SkillRecord sk, ref string __result)
        {

            if (sk.passion != Passion.None)
            {
                VSE.Passions.PassionDef passion = Passions.PassionManager.PassionToDef(sk.passion);
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine();
                stringBuilder.AppendLine();
                stringBuilder.AppendLine("VSE_Passion".Translate(passion.LabelCap).AsTipTitle() + " - " + passion.description.CapitalizeFirst());
                __result += stringBuilder.ToString();
            }


        }


    }













}

