using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace VSE.Expertise;

public static class ExpertisePatches
{
    public static bool ForceVanillaSkills;
    private static readonly Type inner = AccessTools.FirstInner(typeof(CharacterCardUtility), type => AccessTools.Method(type, "<DoLeftSection>b__3") != null);

    public static void Do(Harmony harm)
    {
        var me = typeof(ExpertisePatches);
        harm.Patch(AccessTools.Method(typeof(StatWorker), nameof(StatWorker.GetValueUnfinalized)),
            transpiler: new HarmonyMethod(me, nameof(StatTranspiler)));
        harm.Patch(AccessTools.Method(typeof(StatWorker), nameof(StatWorker.GetExplanationUnfinalized)),
            transpiler: new HarmonyMethod(me, nameof(StatExplainTranspiler)));
        harm.Patch(AccessTools.Constructor(typeof(Pawn_SkillTracker)),
            postfix: new HarmonyMethod(typeof(ExpertiseTrackers), nameof(ExpertiseTrackers.CreateExpertise)));
        harm.Patch(AccessTools.Method(typeof(Pawn_SkillTracker), nameof(Pawn_SkillTracker.ExposeData)),
            postfix: new HarmonyMethod(typeof(ExpertiseTrackers), nameof(ExpertiseTrackers.SaveExpertise)));
        harm.Patch(AccessTools.Method(typeof(SkillRecord), nameof(SkillRecord.Learn)), postfix: new HarmonyMethod(me, nameof(PostLearn)));
        harm.Patch(AccessTools.Method(typeof(CharacterCardUtility), nameof(CharacterCardUtility.DrawCharacterCard)),
            transpiler: new HarmonyMethod(me, nameof(CharacterCardTranspiler)));
        harm.Patch(AccessTools.Method(typeof(CharacterCardUtility), nameof(CharacterCardUtility.DoLeftSection)),
            transpiler: new HarmonyMethod(me, nameof(ExpertiseTitleTranspiler)));
        harm.Patch(AccessTools.Method(inner, "<DoLeftSection>b__3"),
            transpiler: new HarmonyMethod(me, nameof(ExpertiseTitleTranspiler2)));
        harm.Patch(AccessTools.Method(typeof(SkillUI), nameof(SkillUI.DrawSkillsOf)), new HarmonyMethod(me, nameof(DrawExpertiseToo)));
        harm.Patch(AccessTools.Method(typeof(ITab_Pawn_Character), nameof(ITab_Pawn_Character.FillTab)),
            postfix: new HarmonyMethod(me, nameof(DoCharacterCardExtras)));
        harm.Patch(AccessTools.Method(typeof(CharacterCardUtility), nameof(CharacterCardUtility.PawnCardSize)),
            postfix: new HarmonyMethod(me, nameof(PawnCardSize_Postfix)));
    }

    public static bool DrawExpertiseToo(Pawn p, Vector2 offset, SkillUI.SkillDrawMode mode)
    {
        if (ForceVanillaSkills) return true;
        if (p.DevelopmentalStage.Baby()) return true;
        if (!p.Expertise().HasExpertise()) return true;
        ExpertiseUIUtility.DrawSkillsAndExpertiseOf(p, offset, mode);
        return false;
    }

    public static void DoCharacterCardExtras(ITab_Pawn_Character __instance)
    {
        if (Current.ProgramState == ProgramState.Playing && ExpertiseUIUtility.ShowExpertise)
            Find.WindowStack.ImmediateWindow(8931795,
                new Rect(new Vector2(__instance.TabRect.width, __instance.TabRect.y), ExpertiseUIUtility.ExpertisePanelSize(__instance.PawnToShowInfoAbout)),
                WindowLayer.GameUI,
                () =>
                {
                    if (__instance.SelThing is not Pawn or Corpse) return;
                    ExpertiseUIUtility.DoExpertisePanel(new Rect(new Vector2(), ExpertiseUIUtility.ExpertisePanelSize(__instance.PawnToShowInfoAbout)),
                        __instance.PawnToShowInfoAbout);
                });
    }

    public static IEnumerable<CodeInstruction> ExpertiseTitleTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var info = AccessTools.Constructor(typeof(List<CharacterCardUtility.LeftRectSection>));
        var label = generator.DefineLabel();
        foreach (var instruction in instructions)
            if (instruction.opcode == OpCodes.Newobj && instruction.OperandIs(info))
            {
                yield return new CodeInstruction(OpCodes.Ldarg_2).WithLabels(instruction.ExtractLabels());
                yield return CodeInstruction.Call(typeof(ExpertiseTrackers), nameof(ExpertiseTrackers.Expertise), new[] { typeof(Pawn) });
                yield return CodeInstruction.Call(typeof(ExpertiseTracker), nameof(ExpertiseTracker.HasExpertise));
                yield return new CodeInstruction(OpCodes.Brfalse, label);
                yield return new CodeInstruction(OpCodes.Ldloc_3);
                yield return new CodeInstruction(OpCodes.Ldc_R4, 22f);
                yield return new CodeInstruction(OpCodes.Add);
                yield return new CodeInstruction(OpCodes.Stloc_3);
                yield return instruction.WithLabels(label);
            }
            else
                yield return instruction;
    }

    public static IEnumerable<CodeInstruction> ExpertiseTitleTranspiler2(IEnumerable<CodeInstruction> instructions)
    {
        var finishedLoop = false;
        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Leave_S) finishedLoop = true;

            if (instruction.opcode == OpCodes.Ldarg_0 && finishedLoop)
            {
                finishedLoop = false;
                yield return new CodeInstruction(OpCodes.Ldarg_0).WithLabels(instruction.ExtractLabels());
                yield return CodeInstruction.LoadField(inner, "pawn");
                yield return new CodeInstruction(OpCodes.Ldarga, 1);
                yield return CodeInstruction.Call(typeof(Rect), "get_x");
                yield return new CodeInstruction(OpCodes.Ldloca, 0);
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldflda, AccessTools.Field(inner, "leftRect"));
                yield return CodeInstruction.Call(typeof(Rect), "get_width");
                yield return CodeInstruction.Call(typeof(ExpertiseUIUtility), nameof(ExpertiseUIUtility.DoExpertiseTitle));
            }

            yield return instruction;
        }
    }

    public static void PostLearn(SkillRecord __instance, float xp)
    {
        if (xp <= 0f) return;
        foreach (var expertise in __instance.pawn.Expertise().AllExpertise)
            if (expertise.def.skill == __instance.def)
                expertise.Learn(xp);
    }

    public static IEnumerable<CodeInstruction> CharacterCardTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var codes = instructions.ToList();
        var idx1 = codes.FindIndex(ins => ins.IsStloc() && ins.operand is LocalBuilder { LocalIndex: 20 });
        var list = codes.GetRange(idx1 + 1, 2).Select(ins => ins.Clone()).ToList();
        codes.InsertRange(idx1 + 1, list.Concat(new List<CodeInstruction>
        {
            new(OpCodes.Ldloca, 20),
            CodeInstruction.Call(typeof(ExpertiseUIUtility), nameof(ExpertiseUIUtility.DoOpenExpertiseButton))
        }));
        return codes;
    }

    public static IEnumerable<CodeInstruction> StatTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var codes = instructions.ToList();
        var info = AccessTools.Field(typeof(Pawn), nameof(Pawn.skills));
        var idx1 = FindIfJumpIndex(codes, 0, info);
        var label = RewriteJump(codes, generator, 0, AccessTools.Field(typeof(StatDef), nameof(StatDef.skillNeedOffsets)));
        if (label is null) throw new Exception("Failed to find jump location");
        codes.InsertRange(idx1 - 1, new[]
        {
            new CodeInstruction(OpCodes.Ldloc_1).WithLabels(label.Value),
            CodeInstruction.Call(typeof(ExpertiseTrackers), nameof(ExpertiseTrackers.Expertise), new[] { typeof(Pawn) }),
            new CodeInstruction(OpCodes.Ldarg_0),
            CodeInstruction.LoadField(typeof(StatWorker), "stat"),
            new CodeInstruction(OpCodes.Ldloca, 0),
            CodeInstruction.Call(typeof(ExpertiseTracker), nameof(ExpertiseTracker.OffsetStat))
        });
        var idx2 = FindIfJumpIndex(codes, idx1, info);
        label = RewriteJump(codes, generator, idx1, AccessTools.Field(typeof(StatDef), nameof(StatDef.skillNeedFactors)));
        if (label is null) throw new Exception("Failed to find jump location");
        codes.InsertRange(idx2 - 1, new[]
        {
            new CodeInstruction(OpCodes.Ldloc_1).WithLabels(label.Value),
            CodeInstruction.Call(typeof(ExpertiseTrackers), nameof(ExpertiseTrackers.Expertise), new[] { typeof(Pawn) }),
            new CodeInstruction(OpCodes.Ldarg_0),
            CodeInstruction.LoadField(typeof(StatWorker), "stat"),
            new CodeInstruction(OpCodes.Ldloca, 0),
            CodeInstruction.Call(typeof(ExpertiseTracker), nameof(ExpertiseTracker.MultiplyStat))
        });
        return codes;
    }

    public static IEnumerable<CodeInstruction> StatExplainTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var codes = instructions.ToList();
        var info = AccessTools.Field(typeof(Pawn), nameof(Pawn.skills));
        var idx1 = FindIfJumpIndex(codes, 0, info);
        var label = RewriteJump(codes, generator, 0, AccessTools.Field(typeof(StatDef), nameof(StatDef.skillNeedOffsets)));
        if (label is null) throw new Exception("Failed to find jump location");
        codes.InsertRange(idx1, new[]
        {
            new CodeInstruction(OpCodes.Ldloc_2).WithLabels(label.Value),
            CodeInstruction.Call(typeof(ExpertiseTrackers), nameof(ExpertiseTrackers.Expertise), new[] { typeof(Pawn) }),
            new CodeInstruction(OpCodes.Ldarg_0),
            CodeInstruction.LoadField(typeof(StatWorker), "stat"),
            new CodeInstruction(OpCodes.Ldloc_0),
            CodeInstruction.Call(typeof(ExpertiseTracker), nameof(ExpertiseTracker.OffsetStatExplain))
        });
        var idx2 = FindIfJumpIndex(codes, idx1, info);
        label = RewriteJump(codes, generator, idx1, AccessTools.Field(typeof(StatDef), nameof(StatDef.skillNeedFactors)));
        if (label is null) throw new Exception("Failed to find jump location");
        codes.InsertRange(idx2, new[]
        {
            new CodeInstruction(OpCodes.Ldloc_2).WithLabels(label.Value),
            CodeInstruction.Call(typeof(ExpertiseTrackers), nameof(ExpertiseTrackers.Expertise), new[] { typeof(Pawn) }),
            new CodeInstruction(OpCodes.Ldarg_0),
            CodeInstruction.LoadField(typeof(StatWorker), "stat"),
            new CodeInstruction(OpCodes.Ldloc_0),
            CodeInstruction.Call(typeof(ExpertiseTracker), nameof(ExpertiseTracker.MultiplyStatExplain))
        });
        return codes;
    }

    public static void PawnCardSize_Postfix(Pawn pawn, ref Vector2 __result)
    {
        if (pawn?.skills?.Expertise() is { AllExpertise: { Count: var count, Count: > 0 } }) __result.y += count * 30f;
        if (__result.y > UI.screenHeight - 30f - 165f) __result.y = UI.screenHeight - 30 - 165f;
    }

    private static int FindIfJumpIndex(List<CodeInstruction> codes, int startIndex, FieldInfo field)
    {
        var idx1 = codes.FindIndex(startIndex, ins => ins.LoadsField(field));
        Label? label = null;
        var idx2 = codes.FindIndex(idx1, ins => ins.Branches(out label));
        if (label is null) return -1;
        return codes.FindIndex(idx2, ins => ins.labels.Contains(label.Value));
    }

    private static Label? RewriteJump(List<CodeInstruction> codes, ILGenerator generator, int startIndex, FieldInfo field)
    {
        var idx1 = codes.FindIndex(startIndex, ins => ins.LoadsField(field));
        Label? label = null;
        var idx2 = codes.FindIndex(idx1, ins => ins.Branches(out label));
        if (label is not null) label = (Label?)(codes[idx2].operand = generator.DefineLabel());
        return label;
    }
}
