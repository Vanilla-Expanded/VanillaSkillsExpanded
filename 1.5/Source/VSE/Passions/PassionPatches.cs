using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using LudeonTK;
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
            new(me, nameof(GenerateSkills_Prefix)));
        harm.Patch(AccessTools.Method(typeof(Scribe_Values), nameof(Scribe_Values.Look), generics: new[] { typeof(Passion) }),
            new(me, nameof(LookPassion)));
        harm.Patch(AccessTools.Method(typeof(InspirationWorker), nameof(InspirationWorker.CommonalityFor)),
            transpiler: new(me, nameof(CommonalityFor_Transpiler)));
        harm.Patch(AccessTools.Method(typeof(SkillRecord), nameof(SkillRecord.LearnRateFactor)),
            new(me, nameof(LearnRateFactor_Prefix)));
        harm.Patch(AccessTools.Method(typeof(SkillUI), nameof(SkillUI.DrawSkill),
                new[] { typeof(SkillRecord), typeof(Rect), typeof(SkillUI.SkillDrawMode), typeof(string) }),
            transpiler: new(me, nameof(DrawSkill_Transpiler)) { after = new[] { "StrangerDangerPatch" } });
        harm.Patch(AccessTools.Method(typeof(ThoughtWorker_PassionateWork), nameof(ThoughtWorker_PassionateWork.CurrentStateInternal)),
            new(me, nameof(CurrentStateInternal_Prefix)));
        harm.Patch(AccessTools.Method(typeof(SkillRecord), nameof(SkillRecord.Interval)),
            transpiler: new(me, nameof(Interval_Transpiler)));
        harm.Patch(AccessTools.Method(typeof(SkillRecord), nameof(SkillRecord.Learn)),
            transpiler: new(me, nameof(Learn_Transpiler)));
        harm.Patch(AccessTools.Method(typeof(StartingPawnUtility), nameof(StartingPawnUtility.FindBestSkillOwner)),
            transpiler: new(me, nameof(FindBestSkillOwner_Transpiler)));
        harm.Patch(AccessTools.Method(typeof(Pawn_SkillTracker), nameof(Pawn_SkillTracker.MaxPassionOfRelevantSkillsFor)),
            transpiler: new(me, nameof(MaxPassion_Transpiler)));
        harm.Patch(AccessTools.Method(typeof(WidgetsWork), "DrawWorkBoxBackground"),
            transpiler: new(me, nameof(DrawWorkBoxBackground_Transpiler)));
        harm.Patch(AccessTools.Method(typeof(SkillUI), nameof(SkillUI.GetLabel)), new(me, nameof(GetLabel_Prefix)));
        harm.Patch(AccessTools.Method(typeof(SkillUI), nameof(SkillUI.GetLearningFactor)), new(me, nameof(GetLearningFactor_Prefix)));
        harm.Patch(AccessTools.Method(typeof(SkillUI), nameof(SkillUI.GetSkillDescription)),
            transpiler: new(me, nameof(GetSkillDescription_Transpiler)));
        harm.Patch(AccessTools.Method(typeof(DebugToolsPawns), nameof(DebugToolsPawns.SetPassion)), new(me, nameof(SetPassion_Prefix)));
        harm.Patch(AccessTools.Method(typeof(PassionExtension), nameof(PassionExtension.IncrementPassion)),
            new(me, nameof(IncrementPassion_Prefix)));
    }

    public static bool GenerateSkills_Prefix(Pawn pawn, PawnGenerationRequest request)
    {
        if (pawn.ageTracker.AgeBiologicalYears < 13) return true;
        if (pawn.skills?.skills == null) return true;
        foreach (var skillDef in DefDatabase<SkillDef>.AllDefs)
        {
            var level = PawnGenerator.FinalLevelOfSkill(pawn, skillDef, request);
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
        if (DebugSettings.fastLearning) return true;
        __result = __instance.LearnRateFactorBase();
        if (!direct)
        {
            __result *= __instance.pawn.GetStatValue(StatDefOf.GlobalLearningFactor);
            if (__instance.def == SkillDefOf.Animals) __result *= __instance.pawn.GetStatValue(StatDefOf.AnimalsLearningFactor);
            if (__instance.LearningSaturatedToday) __result *= ModCompat.MadSkills ? ModCompat.SaturatedXPMultiplier : 0.2f;
        }

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
                yield return new(OpCodes.Ldloc_0);
                yield return new(OpCodes.Ldarg_0);
                yield return CodeInstruction.Call(typeof(PassionManager), nameof(PassionManager.ForgetRateFactor));
                yield return new(OpCodes.Mul);
                yield return new(OpCodes.Stloc_0);
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

    public static bool GetLabel_Prefix(Passion passion, ref string __result)
    {
        __result = PassionManager.PassionToDef(passion).LabelCap;
        return false;
    }

    public static bool GetLearningFactor_Prefix(Passion passion, ref float __result)
    {
        __result = PassionManager.PassionToDef(passion).learnRateFactor;
        return false;
    }

    public static IEnumerable<CodeInstruction> GetSkillDescription_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var codes = instructions.ToList();
        var info2 = AccessTools.PropertyGetter(typeof(SkillRecord), nameof(SkillRecord.TotallyDisabled));
        var idx2 = codes.FindLastIndex(ins => ins.Calls(info2));
        var label = generator.DefineLabel();
        codes[idx2 + 1].operand = label;

        var idx1 = codes.FindIndex(ins => ins.opcode == OpCodes.Ldstr && ins.OperandIs("ChildrenLearn"));
        var idx4 = codes.FindIndex(idx1, ins => ins.opcode == OpCodes.Ldloc_0);
        var labels = codes[idx4].labels.ListFullCopy();
        codes[idx4].labels.Clear();
        codes[idx4].labels.Add(label);
        codes.InsertRange(idx4, new[]
        {
            new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
            new CodeInstruction(OpCodes.Ldloc_0),
            CodeInstruction.Call(typeof(PassionPatches), nameof(AddForgetRateInfo))
        });

        var info1 = AccessTools.Field(typeof(StatDefOf), nameof(StatDefOf.AnimalsLearningFactor));
        var idx3 = codes.FindLastIndex(ins => ins.LoadsField(info1));
        var idx5 = codes.FindIndex(idx3, ins => ins.opcode == OpCodes.Ldarg_0);
        labels = codes[idx5].labels.ListFullCopy();
        codes[idx5].labels.Clear();
        codes.InsertRange(idx5, new[]
        {
            new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
            new CodeInstruction(OpCodes.Ldloc_0),
            CodeInstruction.Call(typeof(PassionPatches), nameof(AddLearnRateOtherInfo))
        });

        var info3 = AccessTools.Method(typeof(SkillUI), nameof(SkillUI.GetLearningFactor));
        var idx6 = codes.FindIndex(ins => ins.Calls(info3));
        var idx7 = codes.FindLastIndex(idx6, ins => ins.opcode == OpCodes.Ldarg_0);
        codes.RemoveRange(idx7 + 1, idx6 - idx7);
        codes.Insert(idx7 + 1, CodeInstruction.Call(typeof(LearnRateFactorCache), nameof(LearnRateFactorCache.LearnRateFactorBase)));
        return codes;
    }

    public static void AddLearnRateOtherInfo(SkillRecord sk, StringBuilder builder)
    {
        if (SkillsMod.Settings.CriticalEffectPassions || PassionManager.PassionToDef(sk.passion).isBad)
            foreach (var (record, passion) in from record in sk.pawn.skills.skills.Except(sk)
                     let passion = PassionManager.PassionToDef(record.passion)
                     where !Mathf.Approximately(passion.learnRateFactorOther, 1f)
                     select (record, passion))
                builder.AppendLine("  - " + record.def.LabelCap + ": " + passion.LabelCap + ": x" + passion.learnRateFactorOther.ToStringPercent("F0"));
    }

    public static void AddForgetRateInfo(SkillRecord sk, StringBuilder builder)
    {
        builder.AppendLine();
        builder.AppendLineTagged(("VSE.ForgetSpeed".Translate() + ": ").AsTipTitle() + sk.ForgetRateFactor().ToStringPercent());
        builder.AppendLine("  - " + "StatsReport_BaseValue".Translate() + ": " + 1f.ToStringPercent());
        builder.AppendLine("  - " + sk.passion.GetLabel() + ": x" + sk.passion.GetForgetRateFactor().ToStringPercent("F0"));
    }

    public static float GetForgetRateFactor(this Passion passion) => PassionManager.PassionToDef(passion).forgetRateFactor;

    public static bool SetPassion_Prefix(ref List<DebugActionNode> __result)
    {
        __result = new();
        foreach (var skill in DefDatabase<SkillDef>.AllDefs)
        {
            var debugActionNode = new DebugActionNode(skill.defName);
            foreach (var passion in DefDatabase<PassionDef>.AllDefs)
                debugActionNode.AddChild(new(passion.defName, DebugActionType.ToolMapForPawns)
                {
                    pawnAction = delegate(Pawn p)
                    {
                        if (p.skills != null)
                        {
                            p.skills.GetSkill(skill).passion = (Passion)passion.index;
                            DebugActionsUtility.DustPuffFrom(p);
                        }
                    }
                });

            __result.Add(debugActionNode);
        }

        return false;
    }

    public static bool IncrementPassion_Prefix(Passion passion, ref Passion __result)
    {
        __result = passion.ChangePassion(1);
        return false;
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
        if (!codes[idx6].Branches(out var label) || label == null) throw new("Failed to find jump");
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
                ble ? new(OpCodes.Brfalse, label) : new CodeInstruction(OpCodes.Brtrue, label)
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
        if (label == null) throw new("Failed to find jump");
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
