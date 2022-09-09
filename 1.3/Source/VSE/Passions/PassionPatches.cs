using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace VSE.Passions;

public static class PassionPatches
{
    public static void Do(Harmony harm)
    {
        var me = typeof(PassionPatches);
        harm.Patch(AccessTools.Method(typeof(PawnGenerator), nameof(PawnGenerator.GenerateSkills)),
            new HarmonyMethod(me, nameof(GenerateSkills_Prefix)));
        harm.Patch(AccessTools.Method(typeof(Scribe_Values), nameof(Scribe_Values.Look), generics: new[] { typeof(Passion) }),
            new HarmonyMethod(me, nameof(LookPassion)));
        harm.Patch(AccessTools.Method(typeof(InspirationWorker), nameof(InspirationWorker.CommonalityFor)),
            transpiler: new HarmonyMethod(me, nameof(CommonalityFor_Transpiler)));
        harm.Patch(AccessTools.Method(typeof(SkillRecord), nameof(SkillRecord.LearnRateFactor)),
            new HarmonyMethod(me, nameof(LearnRateFactor_Prefix)));
        harm.Patch(AccessTools.Method(typeof(SkillUI), nameof(SkillUI.DrawSkill),
                new[] { typeof(SkillRecord), typeof(Rect), typeof(SkillUI.SkillDrawMode), typeof(string) }),
            transpiler: new HarmonyMethod(me, nameof(DrawSkill_Transpiler)) { after = new[] { "StrangerDangerPatch" } });
        harm.Patch(AccessTools.Method(typeof(SkillUI), nameof(SkillUI.GetSkillDescription)),
            transpiler: new HarmonyMethod(me, nameof(SkillDescription_Transpiler)));
        harm.Patch(AccessTools.Method(typeof(ThoughtWorker_PassionateWork), nameof(ThoughtWorker_PassionateWork.CurrentStateInternal)),
            new HarmonyMethod(me, nameof(CurrentStateInternal_Prefix)));
        harm.Patch(AccessTools.Method(typeof(SkillRecord), nameof(SkillRecord.Interval)),
            transpiler: new HarmonyMethod(me, nameof(Interval_Transpiler)));
        harm.Patch(AccessTools.Method(typeof(SkillRecord), nameof(SkillRecord.Learn)),
            transpiler: new HarmonyMethod(me, nameof(Learn_Transpiler)));
        harm.Patch(AccessTools.Method(typeof(Page_ConfigureStartingPawns), nameof(Page_ConfigureStartingPawns.FindBestSkillOwner)),
            transpiler: new HarmonyMethod(me, nameof(FindBestSkillOwner_Transpiler)));
        harm.Patch(AccessTools.Method(typeof(Pawn_SkillTracker), nameof(Pawn_SkillTracker.MaxPassionOfRelevantSkillsFor)),
            transpiler: new HarmonyMethod(me, nameof(MaxPassion_Transpiler)));
        harm.Patch(AccessTools.Method(typeof(WidgetsWork), "DrawWorkBoxBackground"),
            transpiler: new HarmonyMethod(me, nameof(DrawWorkBoxBackground_Transpiler)));
    }

    public static bool GenerateSkills_Prefix(Pawn pawn)
    {
        if (pawn.skills == null) return true;
        foreach (var skillDef in DefDatabase<SkillDef>.AllDefs)
        {
            var level = PawnGenerator.FinalLevelOfSkill(pawn, skillDef);
            pawn.skills.GetSkill(skillDef).Level = level;
        }

        var num = 5f + Mathf.Clamp(Rand.Gaussian(), -4f, 4f);
        var hasCritical = false;
        if (pawn.story?.traits != null)
            foreach (var skillRecord in from skillRecord in pawn.skills.skills
                     from trait in pawn.story.traits.allTraits
                     where trait.def.RequiresPassion(skillRecord.def)
                     select skillRecord)
            {
                var hasCriticalInt = hasCritical;
                var passionDef = DefDatabase<PassionDef>.AllDefs
                    .Where(def => !def.isBad && (SkillsMod.Settings.AllowMultipleCritical || !hasCriticalInt || !def.IsCritical))
                    .RandomElementByWeight(def => def.commonality);
                if (passionDef.IsCritical) hasCritical = true;
                skillRecord.passion = (Passion)passionDef.index;
                num -= 1f;
            }

        while (num >= 1f)
        {
            var hasCriticalInt = hasCritical;
            var passion = DefDatabase<PassionDef>.AllDefs
                .Where(def => SkillsMod.Settings.AllowMultipleCritical || !hasCriticalInt || !def.IsCritical)
                .RandomElementByWeight(def => def.commonality);
            SkillRecord skillRecord;
            if (passion.isBad)
            {
                num += 1f;
                var max = pawn.skills.skills.Max(sr => sr.Level);
                skillRecord = pawn.skills.skills.RandomElementByWeight(sr => max - sr.Level);
            }
            else
            {
                num -= 1f;
                skillRecord = pawn.skills.skills.RandomElementByWeight(sr => sr.Level);
            }

            if (passion.IsCritical) hasCritical = true;
            skillRecord.passion = (Passion)passion.index;
        }

        return false;
    }

    public static bool LookPassion(ref Passion value, string label, Passion defaultValue = Passion.None, bool forceSave = false)
    {
        if (Scribe.mode == LoadSaveMode.Saving) Scribe.saver.WriteElement(label, PassionManager.Passions[(ushort)value].defName);
        else if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            var subNode = Scribe.loader.curXmlParent[label];
            if (subNode == null) value = defaultValue;
            else
            {
                var xmlAttribute = subNode.Attributes["IsNull"];
                if (xmlAttribute != null && xmlAttribute.Value.ToLower() == "true") value = Passion.None;
                else
                {
                    var def = DefDatabase<PassionDef>.GetNamedSilentFail(subNode.InnerText);
                    if (def == null) value = defaultValue;
                    else value = (Passion)def.index;
                }
            }
        }

        return false;
    }

    public static IEnumerable<CodeInstruction> CommonalityFor_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        var idx1 = codes.FindIndex(ins => ins.opcode == OpCodes.Switch);
        var idx2 = codes.FindIndex(idx1, ins => ins.opcode == OpCodes.Ldc_R4 && ins.operand is 5f);
        var idx3 = codes.FindIndex(idx2, ins => ins.opcode == OpCodes.Stloc_0);
        codes.RemoveRange(idx1, idx3 - idx1 + 1);
        codes.InsertRange(idx1, new[]
        {
            CodeInstruction.Call(typeof(PassionManager), nameof(PassionManager.PassionToDef)),
            CodeInstruction.LoadField(typeof(PassionDef), nameof(PassionDef.inspirationCommonality)),
            new CodeInstruction(OpCodes.Ldloc_0),
            CodeInstruction.Call(typeof(Mathf), nameof(Mathf.Max), new[] { typeof(float), typeof(float) }),
            new CodeInstruction(OpCodes.Stloc_0)
        });
        return codes;
    }

    public static bool LearnRateFactor_Prefix(bool direct, SkillRecord __instance, ref float __result)
    {
        if (DebugSettings.fastLearning && !ModCompat.InsaneSkills) return true;
        __result = __instance.LearnRateFactorBase();
        if (!direct)
        {
            __result *= __instance.pawn.GetStatValue(StatDefOf.GlobalLearningFactor);
            if (__instance.def == SkillDefOf.Animals) __result *= __instance.pawn.GetStatValue(StatDefOf.AnimalsLearningFactor);
            if (!ModCompat.InsaneSkills && __instance.LearningSaturatedToday) __result *= ModCompat.MadSkills ? ModCompat.SaturatedXPMultiplier : 0.2f;
        }

        if (ModCompat.InsaneSkills && ModCompat.ValueSkillCap > 0f)
            __result = Math.Min(1f / (Math.Max(__instance.xpSinceMidnight, 0f) / __result) * ModCompat.ValueSkillCap, 1f);

        return false;
    }

    public static IEnumerable<CodeInstruction> DrawSkill_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        var info = AccessTools.Field(typeof(SkillRecord), nameof(SkillRecord.passion));
        var idx1 = codes.FindIndex(ins => ins.LoadsField(info) || (ModCompat.StrangerDanger && ins.Calls(ModCompat.SD_passion)));
        var idx2 = codes.FindIndex(idx1, ins => ins.opcode == OpCodes.Stloc_S);
        codes.RemoveRange(idx1 + 1, idx2 - idx1 - 1);
        codes.InsertRange(idx1 + 1, new[]
        {
            CodeInstruction.Call(typeof(PassionManager), nameof(PassionManager.PassionToDef)),
            new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(PassionDef), nameof(PassionDef.Icon)))
        });
        return codes;
    }

    public static IEnumerable<CodeInstruction> SkillDescription_Transpiler(IEnumerable<CodeInstruction> instructions) =>
        SwitchReplacer(instructions, load => new[]
        {
            new CodeInstruction(OpCodes.Ldloc_0),
            load,
            CodeInstruction.Call(typeof(PassionManager), nameof(PassionManager.PassionToDef)),
            new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(PassionDef), nameof(PassionDef.FullDescription)))
        }, ins => ins.opcode == OpCodes.Callvirt);

    public static bool CurrentStateInternal_Prefix(Pawn p, ref ThoughtState __result)
    {
        if (p?.jobs?.curDriver?.ActiveSkill is not { } def || p.skills?.GetSkill(def) is not { passion: var passion } ||
            PassionManager.PassionToDef(passion).isBad) return true;
        __result = ThoughtState.ActiveAtStage(1);
        return false;
    }

    public static IEnumerable<CodeInstruction> Interval_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            yield return instruction;
            if (instruction.opcode == OpCodes.Stloc_0)
            {
                yield return new CodeInstruction(OpCodes.Ldloc_0);
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return CodeInstruction.Call(typeof(PassionManager), nameof(PassionManager.ForgetRateFactor));
                yield return new CodeInstruction(OpCodes.Mul);
                yield return new CodeInstruction(OpCodes.Stloc_0);
            }
        }
    }

    public static IEnumerable<CodeInstruction> FindBestSkillOwner_Transpiler(IEnumerable<CodeInstruction> instructions) =>
        CompareReplacer(instructions, ins => ins.opcode == OpCodes.Ble_S);

    public static IEnumerable<CodeInstruction> MaxPassion_Transpiler(IEnumerable<CodeInstruction> instructions) =>
        CompareReplacer(instructions, ins => ins.opcode == OpCodes.Ble_S);

    public static IEnumerable<CodeInstruction> Learn_Transpiler(IEnumerable<CodeInstruction> instructions) => IsBadReplacer(instructions);

    public static IEnumerable<CodeInstruction> DrawWorkBoxBackground_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        var idx1 = codes.FindIndex(ins => ins.opcode == OpCodes.Ldc_I4_1) - 1;
        var info1 = AccessTools.PropertyGetter(typeof(Color), nameof(Color.white));
        var idx2 = codes.FindLastIndex(ins => ins.Calls(info1));
        codes.RemoveRange(idx1, idx2 - idx1);
        codes.InsertRange(idx1, new[]
        {
            new CodeInstruction(OpCodes.Ldloc, 5),
            new CodeInstruction(OpCodes.Ldloc, 4),
            CodeInstruction.Call(typeof(PassionManager), nameof(PassionManager.PassionToDef)),
            new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(PassionDef), nameof(PassionDef.WorkBoxIcon))),
            CodeInstruction.Call(typeof(GUI), nameof(GUI.DrawTexture), new[] { typeof(Rect), typeof(Texture) })
        });
        return codes;
    }

    internal static IEnumerable<CodeInstruction> SwitchReplacer(IEnumerable<CodeInstruction> instructions,
        Func<CodeInstruction, IEnumerable<CodeInstruction>> getReplace, Predicate<CodeInstruction> findPreserve = null,
        Predicate<CodeInstruction> additionalCheck = null)
    {
        var codes = instructions.ToList();
        var idx1 = codes.FindIndex(ins => ins.opcode == OpCodes.Switch && (additionalCheck == null || additionalCheck(ins)));
        var label1 = ((Label[])codes[idx1].operand)[0];
        var idx5 = codes.FindIndex(idx1, ins => ins.labels.Contains(label1));
        var idx6 = codes.FindIndex(idx5, ins => ins.opcode == OpCodes.Br_S);
        if (!codes[idx6].Branches(out var label) || label == null) throw new Exception("Failed to find jump");
        var idx2 = codes.FindIndex(idx1, ins => ins.labels.Contains(label.Value));
        var idx3 = findPreserve == null ? idx2 : codes.FindLastIndex(idx2, findPreserve);
        var idx4 = codes.FindLastIndex(idx1, ins => ins.IsLdloc());
        var load = codes[idx4].Clone();
        codes.RemoveRange(idx4, idx3 - idx4);
        codes.InsertRange(idx4, getReplace(load));
        return codes;
    }

    public static IEnumerable<CodeInstruction> CompareReplacer(IEnumerable<CodeInstruction> instructions, Predicate<CodeInstruction> doReplace,
        bool ble = true, bool allowEq = false)
    {
        var codes = instructions.ToList();
        int idx1;
        while ((idx1 = codes.FindIndex(doReplace)) >= 0)
        {
            var label = (Label)codes[idx1].operand;
            codes.RemoveAt(idx1);
            codes.InsertRange(idx1, new[]
            {
                CodeInstruction.Call(typeof(PassionPatches), allowEq ? nameof(ComparePassionsEq) : nameof(ComparePassions)),
                ble ? new CodeInstruction(OpCodes.Brfalse, label) : new CodeInstruction(OpCodes.Brtrue, label)
            });
        }

        return codes;
    }

    public static bool ComparePassions(Passion passion1, Passion passion2) =>
        PassionManager.PassionToDef(passion1).learnRateFactor > PassionManager.PassionToDef(passion2).learnRateFactor;

    public static bool ComparePassionsEq(Passion passion1, Passion passion2) =>
        PassionManager.PassionToDef(passion1).learnRateFactor >= PassionManager.PassionToDef(passion2).learnRateFactor;

    private static IEnumerable<CodeInstruction> IsBadReplacer(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        var info = AccessTools.Field(typeof(SkillRecord), nameof(SkillRecord.passion));
        var idx1 = codes.FindIndex(ins => ins.LoadsField(info));
        Label? label = null;
        var idx2 = codes.FindIndex(idx1, ins => ins.Branches(out label));
        if (label == null) throw new Exception("Failed to find jump");
        codes.RemoveRange(idx1 + 1, idx2 - idx1);
        codes.InsertRange(idx1 + 1, new[]
        {
            CodeInstruction.Call(typeof(PassionManager), nameof(PassionManager.PassionToDef)),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PassionDef), nameof(PassionDef.isBad))),
            new CodeInstruction(OpCodes.Brfalse, label)
        });
        return codes;
    }
}