using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace VSE.Stats;

public static class StatPatches
{
    public static void Do(Harmony harm)
    {
        var myType = typeof(StatPatches);
        foreach (var methodInfo in typeof(VerbProperties).GetMethods(AccessTools.all)
                    .Where(mi => mi.Name == "AdjustedArmorPenetration" && mi.GetParameters().Length > 2))
            harm.Patch(methodInfo, postfix: new(myType, nameof(AdjustArmorPenetration)));
        foreach (var methodInfo in typeof(VerbProperties).GetMethods(AccessTools.all)
                    .Where(mi => mi.Name == "AdjustedCooldown" && mi.GetParameters().Length > 2))
            harm.Patch(methodInfo, postfix: new(myType, nameof(AdjustMeleeCooldown)));
        harm.Patch(AccessTools.Method(typeof(Pawn_MindState), nameof(Pawn_MindState.CheckStartMentalStateBecauseRecruitAttempted)),
            transpiler: new(myType, nameof(AttackOnTameFailTranspiler)));
        harm.Patch(AccessTools.Method(typeof(GenRecipe), nameof(GenRecipe.PostProcessProduct)),
            transpiler: new(myType, nameof(CraftingQualityTranspiler)));
        harm.Patch(AccessTools.Method(AccessTools.Inner(typeof(JobDriver_AffectRoof), "<>c__DisplayClass12_0"), "<MakeNewToils>b__1"),
            transpiler: new(myType, nameof(RoofStatTranspiler)));
        harm.Patch(AccessTools.PropertyGetter(typeof(JobDriver_RemoveFloor), nameof(JobDriver_RemoveFloor.SpeedStat)),
            transpiler: new(myType, nameof(FloorStatTranspiler)));
        harm.Patch(AccessTools.Method(AccessTools.Inner(typeof(JobDriver_ConstructFinishFrame), "<>c__DisplayClass6_0"), "<MakeNewToils>b__1"),
            transpiler: new(myType, nameof(FloorStatOptionTranspiler)));
        harm.Patch(AccessTools.Method(AccessTools.Inner(typeof(JobDriver_Repair), "<>c__DisplayClass8_0"), "<MakeNewToils>b__1"),
            transpiler: new(myType, nameof(RepairStatTranspiler)));
        harm.Patch(AccessTools.Method(typeof(Frame), nameof(Frame.CompleteConstruction)),
            transpiler: new(myType, nameof(ConstructionQualityTranspiler)));
        harm.Patch(AccessTools.Method(typeof(Mineable), nameof(Mineable.TrySpawnYield), new[] { typeof(Map), typeof(bool), typeof(Pawn) }),
            transpiler: new(myType, nameof(RockChunkChanceTranspiler)));
        harm.Patch(AccessTools.Method(typeof(InteractionWorker_RecruitAttempt), nameof(InteractionWorker_RecruitAttempt.Interacted)),
            transpiler: new(myType, nameof(RecruitStatTranspiler)));
        harm.Patch(AccessTools.Method(typeof(PeaceTalks), nameof(PeaceTalks.GetBadOutcomeWeightFactor), new[] { typeof(Pawn), typeof(Caravan) }),
            transpiler: new(myType, nameof(PeaceTalksStatTranspiler)));
    }

    public static void AdjustArmorPenetration(Pawn attacker, ref float __result)
    {
        if (attacker is not null) __result *= attacker.GetStatValue(MoreStatDefOf.VSE_ArmorPenetrationFactor);
    }

    public static void AdjustMeleeCooldown(Pawn attacker, ref float __result, Tool tool)
    {
        if (attacker is not null && tool is not null) __result *= attacker.GetStatValue(MoreStatDefOf.VSE_MeleeCooldownFactor);
    }

    public static IEnumerable<CodeInstruction> AttackOnTameFailTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var info = AccessTools.Field(typeof(RaceProperties), nameof(RaceProperties.manhunterOnTameFailChance));
        foreach (var instruction in instructions)
        {
            yield return instruction;
            if (instruction.LoadsField(info))
            {
                yield return new(OpCodes.Ldarg_1);
                yield return CodeInstruction.LoadField(typeof(MoreStatDefOf), nameof(MoreStatDefOf.VSE_AttackOnFailChanceFactor));
                yield return new(OpCodes.Ldc_I4_1);
                yield return new(OpCodes.Ldc_I4, -1);
                yield return CodeInstruction.Call(typeof(StatExtension), nameof(StatExtension.GetStatValue));
                yield return new(OpCodes.Mul);
            }
        }
    }

    public static IEnumerable<CodeInstruction> CraftingQualityTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var info = AccessTools.Method(typeof(RimWorld.QualityUtility), nameof(RimWorld.QualityUtility.GenerateQualityCreatedByPawn),
            new[] { typeof(Pawn), typeof(SkillDef) });
        foreach (var instruction in instructions)
            if (instruction.Calls(info))
            {
                yield return new(OpCodes.Ldarg_0);
                yield return CodeInstruction.Call(typeof(QualityUtility), nameof(QualityUtility.GenerateQuality));
            }
            else yield return instruction;
    }

    public static IEnumerable<CodeInstruction> ConstructionQualityTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var info = AccessTools.Method(typeof(RimWorld.QualityUtility), nameof(RimWorld.QualityUtility.GenerateQualityCreatedByPawn),
            new[] { typeof(Pawn), typeof(SkillDef) });
        foreach (var instruction in instructions)
            if (instruction.Calls(info))
            {
                yield return new(OpCodes.Ldloc, 3);
                yield return CodeInstruction.Call(typeof(QualityUtility), nameof(QualityUtility.GenerateQuality));
            }
            else yield return instruction;
    }

    public static IEnumerable<CodeInstruction> RoofStatTranspiler(IEnumerable<CodeInstruction> instructions) =>
        instructions.StatReplacer(nameof(StatDefOf.ConstructionSpeed), nameof(MoreStatDefOf.VSE_RoofSpeed));

    public static IEnumerable<CodeInstruction> FloorStatTranspiler(IEnumerable<CodeInstruction> instructions) =>
        instructions.StatReplacer(nameof(StatDefOf.ConstructionSpeed), nameof(MoreStatDefOf.VSE_FloorSpeed));

    public static IEnumerable<CodeInstruction> RepairStatTranspiler(IEnumerable<CodeInstruction> instructions) =>
        instructions.StatReplacer(nameof(StatDefOf.ConstructionSpeed), nameof(MoreStatDefOf.VSE_RepairSpeed));

    public static IEnumerable<CodeInstruction> RecruitStatTranspiler(IEnumerable<CodeInstruction> instructions) =>
        instructions.StatReplacer(nameof(StatDefOf.NegotiationAbility), nameof(MoreStatDefOf.VSE_RecruitRate));

    public static IEnumerable<CodeInstruction> PeaceTalksStatTranspiler(IEnumerable<CodeInstruction> instructions) =>
        instructions.StatReplacer(nameof(StatDefOf.NegotiationAbility), nameof(MoreStatDefOf.VSE_PeaceTalksChance));

    public static IEnumerable<CodeInstruction> RockChunkChanceTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var info = AccessTools.Field(typeof(BuildingProperties), nameof(BuildingProperties.mineableDropChance));
        foreach (var instruction in instructions)
        {
            yield return instruction;
            if (instruction.LoadsField(info))
            {
                yield return new(OpCodes.Ldarg_0);
                yield return CodeInstruction.LoadField(typeof(MoreStatDefOf), nameof(MoreStatDefOf.VSE_RockChunkChance));
                yield return new(OpCodes.Ldc_I4_1);
                yield return new(OpCodes.Ldc_I4, -1);
                yield return CodeInstruction.Call(typeof(StatExtension), nameof(StatExtension.GetStatValue));
                yield return new(OpCodes.Mul);
            }
        }
    }

    public static IEnumerable<CodeInstruction> FloorStatOptionTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var statInfo = "ConstructionSpeed".StatInfo();
        foreach (var instruction in instructions)
            if (instruction.operand is FieldInfo info && info == statInfo)
            {
                yield return new(OpCodes.Ldloc_1);
                yield return CodeInstruction.Call(typeof(StatUtility), nameof(StatUtility.ConstructionStatForFrame));
            }
            else yield return instruction;
    }

    private static IEnumerable<CodeInstruction> StatReplacer(this IEnumerable<CodeInstruction> instructions, string oldStat, string newStat)
    {
        var from = oldStat.StatInfo();
        var to = newStat.StatInfo();
        foreach (var instruction in instructions)
            if (instruction.LoadsField(from)) yield return new(OpCodes.Ldsfld, to);
            else yield return instruction;
    }

    private static FieldInfo StatInfo(this string statName)
    {
        var info = AccessTools.Field(typeof(StatDefOf), statName);
        if (info is not null) return info;
        info = AccessTools.Field(typeof(MoreStatDefOf), statName);
        if (info is not null) return info;
        return null;
    }
}
