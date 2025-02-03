using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;
using System;

using System.Text;
using System.Reflection;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using VSE.Passions;


namespace VSE
{


    public static class VSE_ChoiceLetter_GrowthMoment_MakeChoices_Patch
    {



        public static IEnumerable<CodeInstruction> ChooseCorrectPassionIncrement(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            var info1 = AccessTools.Method(typeof(PassionExtension), nameof(PassionExtension.IncrementPassion));
            var idx1 = codes.FindIndex(ins => ins.Calls(info1));
            codes.RemoveAt(idx1);
            codes.InsertRange(idx1, new[] {
            CodeInstruction.Call(typeof(VSE_ChoiceLetter_GrowthMoment_MakeChoices_Patch), nameof(VSE_ChoiceLetter_GrowthMoment_MakeChoices_Patch.ChooseIncrementedPassion))
            });

            var info2 = AccessTools.Field(typeof(SkillRecord), nameof(SkillRecord.passion));
            var idx2 = codes.FindIndex(ins => ins.LoadsField(info2));
            codes.RemoveRange(idx2-1,17);

            return codes;
        }

        public static Passion ChooseIncrementedPassion(Passion passion)
        {
            PassionDef passionDef = PassionManager.PassionToDef(passion);
            if (passionDef.passionToIncreaseTo != null)
            {
                return (Passion)PassionManager.PassionToDef(passion).passionToIncreaseTo.index;

            }
            else
            {
                return (Passion)PassionDefOf.Major.index;
            }


        }

        public static void AddRandomBadPassion(List<SkillDef> skills, ChoiceLetter_GrowthMoment __instance)
        {
            if (Rand.Chance(SkillsMod.Settings.GrowthMomentRandomPassionsChance/100))
            {
               
                Pawn pawn = __instance.pawn;
                if (pawn.ageTracker.AgeBiologicalYears <= 13)
                {
                    var passion = DefDatabase<PassionDef>.AllDefs
              .Where(def => def.randomForBabies &&
              (def.blockingTraits.NullOrEmpty() || pawn.story?.traits?.allTraits?.Select(x => x.def).ToList().Intersect(def.blockingTraits).Any() == false) &&
              (def.blockingTraitsWithDegree.NullOrEmpty() || def.blockingTraitsWithDegree.TrueForAll(trait => !trait.HasTrait(pawn))) &&
               (def.blockingPrecepts.NullOrEmpty() || pawn.Faction?.ideos?.PrimaryIdeo?.PreceptsListForReading?.Select(x => x.def).ToList().Intersect(def.blockingPrecepts).Any() == false) &&
                (def.blockingGenes.NullOrEmpty() || pawn.genes?.GenesListForReading?.Select(x => x.def).ToList().Intersect(def.blockingGenes).Any() == false) &&
                (def.maxAge == -1 || pawn.ageTracker.AgeBiologicalYears < def.maxAge) &&
                 (def.minAge == -1 || pawn.ageTracker.AgeBiologicalYears > def.minAge) &&
              (def.requiredTraits.NullOrEmpty() || pawn.story?.traits?.allTraits?.Select(x => x.def).ToList().Intersect(def.requiredTraits).Count() == def.requiredTraits.Count()) &&
              (def.requiredTraitsWithDegree.NullOrEmpty() || def.requiredTraitsWithDegree.TrueForAll(trait => trait.HasTrait(pawn))) &&
               (def.requiredPrecepts.NullOrEmpty() || pawn.Faction?.ideos?.PrimaryIdeo?.PreceptsListForReading?.Select(x => x.def).ToList().Intersect(def.requiredPrecepts).Count() == def.requiredPrecepts.Count()) &&
                (def.requiredGenes.NullOrEmpty() || pawn.genes?.GenesListForReading?.Select(x => x.def).ToList().Intersect(def.requiredGenes).Count() == def.requiredGenes.Count())

              )
              .RandomElementByWeight(def => def.commonality);


                    foreach (SkillRecord skill in pawn.skills.skills.InRandomOrder())
                    {
                       
                        if (skill.passion == Passion.None)
                        {
                            skill.passion = (Passion)passion.index;
                           
                            return;
                        }
                    }
                }

               
            }



        }


    }













}

