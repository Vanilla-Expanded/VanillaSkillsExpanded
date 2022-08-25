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

    public static void Do(Harmony harm)
    {
        var myType = typeof(ExpertisePatches);
        harm.Patch(AccessTools.Method(typeof(StatWorker), nameof(StatWorker.GetValueUnfinalized)),
            transpiler: new HarmonyMethod(myType, nameof(StatTranspiler)));
        harm.Patch(AccessTools.Method(typeof(StatWorker), nameof(StatWorker.GetExplanationUnfinalized)),
            transpiler: new HarmonyMethod(myType, nameof(StatExplainTranspiler)));
        harm.Patch(AccessTools.Constructor(typeof(Pawn_SkillTracker)),
            postfix: new HarmonyMethod(typeof(ExpertiseTrackers), nameof(ExpertiseTrackers.CreateExpertise)));
        harm.Patch(AccessTools.Method(typeof(Pawn_SkillTracker), nameof(Pawn_SkillTracker.ExposeData)),
            postfix: new HarmonyMethod(typeof(ExpertiseTrackers), nameof(ExpertiseTrackers.SaveExpertise)));
        harm.Patch(AccessTools.Method(typeof(SkillRecord), nameof(SkillRecord.Learn)), postfix: new HarmonyMethod(myType, nameof(PostLearn)));
        harm.Patch(AccessTools.Method(typeof(CharacterCardUtility), nameof(CharacterCardUtility.DrawCharacterCard)),
            transpiler: new HarmonyMethod(myType, nameof(CharacterCardTranspiler)));
        harm.Patch(AccessTools.Method(AccessTools.Inner(typeof(CharacterCardUtility), "<>c__DisplayClass15_1"), "<DrawCharacterCard>b__25"),
            transpiler: new HarmonyMethod(myType, nameof(ExpertiseTitleTranspiler)));
        harm.Patch(AccessTools.Method(typeof(SkillUI), nameof(SkillUI.DrawSkillsOf)), new HarmonyMethod(myType, nameof(DrawExpertiseToo)));
        harm.Patch(AccessTools.Method(typeof(ITab_Pawn_Character), nameof(ITab_Pawn_Character.FillTab)),
            postfix: new HarmonyMethod(myType, nameof(DoCharacterCardExtras)));
    }

    public static bool DrawExpertiseToo(Pawn p, Vector2 offset, SkillUI.SkillDrawMode mode)
    {
        if (ForceVanillaSkills) return true;
        if (!p.Expertise().HasExpertise()) return true;
        ExpertiseUIUtility.DrawSkillsAndExpertiseOf(p, offset, mode);
        return false;
    }

    public static void DoCharacterCardExtras(ITab_Pawn_Character __instance)
    {
        if (Current.ProgramState == ProgramState.Playing && ExpertiseUIUtility.ShowExpertise)
            Find.WindowStack.ImmediateWindow(8931795,
                new Rect(new Vector2(__instance.TabRect.width, __instance.TabRect.y), ExpertiseUIUtility.ExpertisePanelSize),
                WindowLayer.GameUI,
                () =>
                {
                    if (__instance.SelThing is not Pawn or Corpse) return;
                    ExpertiseUIUtility.DoExpertisePanel(new Rect(new Vector2(), ExpertiseUIUtility.ExpertisePanelSize), __instance.PawnToShowInfoAbout);
                });
    }

    public static IEnumerable<CodeInstruction> ExpertiseTitleTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Ret)
            {
                yield return new CodeInstruction(OpCodes.Ldarg_1).WithLabels(instruction.ExtractLabels());
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return CodeInstruction.LoadField(AccessTools.Inner(typeof(CharacterCardUtility), "<>c__DisplayClass15_1"), "CS$<>8__locals1");
                yield return CodeInstruction.LoadField(AccessTools.Inner(typeof(CharacterCardUtility), "<>c__DisplayClass15_0"), "pawn");
                yield return new CodeInstruction(OpCodes.Ldloca, 0);
                yield return CodeInstruction.Call(typeof(ExpertiseUIUtility), nameof(ExpertiseUIUtility.DoExpertiseTitle));
            }

            yield return instruction;
        }
    }

    public static void PostLearn(SkillRecord __instance, float xp)
    {
        foreach (var expertise in __instance.pawn.Expertise().AllExpertise)
            if (expertise.def.skill == __instance.def)
                expertise.Learn(xp);
    }

    public static IEnumerable<CodeInstruction> CharacterCardTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var codes = instructions.ToList();
        var idx1 = codes.FindIndex(ins => ins.IsStloc() && ins.operand is LocalBuilder { LocalIndex: 15 });
        var list = codes.GetRange(idx1 + 1, 2).Select(ins => ins.Clone()).ToList();
        codes.InsertRange(idx1 + 1, list.Concat(new List<CodeInstruction>
        {
            new(OpCodes.Ldloca, 15),
            CodeInstruction.Call(typeof(ExpertiseUIUtility), nameof(ExpertiseUIUtility.DoOpenExpertiseButton))
        }));
        var idx2 = codes.FindIndex(ins => ins.IsStloc() && ins.operand is LocalBuilder { LocalIndex: 33 });
        var list2 = codes.GetRange(idx2 + 1, 4).Select(ins => ins.Clone()).ToList();
        var label = generator.DefineLabel();
        codes[idx2 + 1].labels.Add(label);
        codes.InsertRange(idx2 + 1, list2.Concat(new List<CodeInstruction>
        {
            CodeInstruction.Call(typeof(ExpertiseUIUtility), nameof(ExpertiseUIUtility.HasExpertiseToDraw)),
            new(OpCodes.Brfalse, label),
            new(OpCodes.Ldloc, 33),
            new(OpCodes.Ldc_R4, 22f),
            new(OpCodes.Add),
            new(OpCodes.Stloc, 33)
        }));
        return codes;
    }

    public static IEnumerable<CodeInstruction> StatTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var codes = instructions.ToList();
        var info = AccessTools.Field(typeof(Pawn), nameof(Pawn.skills));
        var idx1 = FindIfJumpIndex(codes, 0, info);
        var label = LocateJump(codes, 0, AccessTools.Field(typeof(StatDef), nameof(StatDef.skillNeedOffsets)));
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
        label = LocateJump(codes, idx1, AccessTools.Field(typeof(StatDef), nameof(StatDef.skillNeedFactors)));
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
        var label = LocateJump(codes, 0, AccessTools.Field(typeof(StatDef), nameof(StatDef.skillNeedOffsets)));
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
        label = LocateJump(codes, idx1, AccessTools.Field(typeof(StatDef), nameof(StatDef.skillNeedFactors)));
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

    private static int FindIfJumpIndex(List<CodeInstruction> codes, int startIndex, FieldInfo field)
    {
        var idx1 = codes.FindIndex(startIndex, ins => ins.LoadsField(field));
        Label? label = null;
        var idx2 = codes.FindIndex(idx1, ins => ins.Branches(out label));
        if (label is null) return -1;
        return codes.FindIndex(idx2, ins => ins.labels.Contains(label.Value));
    }

    private static Label? LocateJump(List<CodeInstruction> codes, int startIndex, FieldInfo field)
    {
        var idx1 = codes.FindIndex(startIndex, ins => ins.LoadsField(field));
        Label? label = null;
        var idx2 = codes.FindIndex(idx1, ins => ins.Branches(out label));
        if (label is not null)
            foreach (var instruction in codes.Skip(idx2).Where(ins => ins.labels.Contains(label.Value)))
                instruction.labels.Remove(label.Value);
        return label;
    }
}