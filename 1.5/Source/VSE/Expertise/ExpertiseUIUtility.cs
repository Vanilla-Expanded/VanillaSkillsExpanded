﻿using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using VFECore.UItils;
using VSE.Expertise;

namespace VSE;

[StaticConstructorOnStartup]
public static class ExpertiseUIUtility
{
    public static bool ShowExpertise;

    private static readonly Texture2D ExpertiseBarFillTex =
        SolidColorMaterials.NewSolidColorTexture(new Color(144f / 255f, 138f / 255f, 110f / 255f, 0.2f));

    private static readonly Texture2D OpenExpertiseIcon = ContentFinder<Texture2D>.Get("UI/Icon_OpenExpertisePanel");
    private static readonly Texture2D ExpertisePassion = ContentFinder<Texture2D>.Get("UI/Passion_Expertise");

    private static Vector2 expertiseScrollPos;

    private static readonly Dictionary<string, string> truncateCache = new();

    private static Vector2 skillsScrollPos;

    public static Vector2 ExpertisePanelSize(Pawn pawn)
    {
        var size = CharacterCardUtility.PawnCardSize(pawn);
        size.x *= 1.5f;
        size += new Vector2(17f, 17f) * 2f;
        return size;
    }

    public static void DoOpenExpertiseButton(Pawn pawn, ref float x)
    {
        if (pawn.IsColonist)
        {
            var rect = new Rect(x, 0, 30f, 30f);
            if (Widgets.ButtonImage(rect, OpenExpertiseIcon)) ShowExpertise = !ShowExpertise;
            x -= 40f;
        }
    }

    public static void DoExpertiseTitle(Pawn pawn, float x, ref float y, float width)
    {
        var expertiseTracker = pawn.Expertise();
        if (!expertiseTracker.HasExpertise()) return;
        var rect = new Rect(x, y, width, 22f);
        var expertise = expertiseTracker.AllExpertise.MaxBy(record => record.Level);
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(rect, "VSE.Expertise".Translate() + ":");
        var text = expertise.def.LabelCap.Resolve();
        rect.x += 90f;
        rect.width = Text.CalcSize(text).x + 10f;
        var color = GUI.color;
        GUI.color = CharacterCardUtility.StackElementBackground;
        GUI.DrawTexture(rect, BaseContent.WhiteTex);
        GUI.color = color;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(rect, text);
        Text.Anchor = TextAnchor.UpperLeft;
        if (Mouse.IsOver(rect))
        {
            Widgets.DrawHighlight(rect);
            TooltipHandler.TipRegion(rect, expertise.FullDescription());
        }

        y += rect.height;
    }

    public static bool HasExpertiseToDraw(Pawn pawn) => pawn.Expertise().HasExpertise();

    public static void DoExpertisePanel(Rect inRect, Pawn pawn)
    {
        if (Widgets.CloseButtonFor(inRect)) ShowExpertise = false;

        var anchor = Text.Anchor;
        var font = Text.Font;

        inRect.yMin += 20f;
        inRect.xMax -= 5f;
        var textRect = inRect.TakeBottomPart(54f).ContractedBy(7f);
        var expertiseTracker = pawn.Expertise();
        var allExpertise = (from ex in DefDatabase<ExpertiseDef>.AllDefs.Except(expertiseTracker.AllExpertise.Select(ex => ex.def))
            let skill = pawn.skills.GetSkill(ex.skill)
            orderby ex.CanApplyOn(pawn, out _) descending,
                skill.passion descending,
                skill.Level descending,
                skill.def.listOrder descending
            select ex).ToList();
        var viewRect = new Rect(0, 0, inRect.width - 30f, allExpertise.Count * 60f);
        Widgets.BeginScrollView(inRect, ref expertiseScrollPos, viewRect);
        var y = 0f;
        for (var i = 0; i < allExpertise.Count; i++)
        {
            var expertise = allExpertise[i];
            var rect = new Rect(15f, y, viewRect.width - 15f, 55f);
            Widgets.DrawMenuSection(rect);
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Small;
            float width;
            Widgets.Label(rect.TakeLeftPart(rect.width * 0.2f).ContractedBy(5f), expertise.LabelCap);
            Widgets.Label(rect.TakeLeftPart(width = rect.width * 0.75f),
                expertise.description.TruncateHeight(width, rect.height, truncateCache).Colorize(ColoredText.SubtleGrayColor));
            var buttonRect = new Rect(rect.x, rect.y + rect.height / 2f - 15f, rect.width, 30f).ContractedBy(5f);
            if (expertise.CanApplyOn(pawn, out var reason))
            {
                if (Widgets.ButtonText(buttonRect, "VSE.SelectExpertise".Translate()))
                    expertiseTracker.AddExpertise(expertise);
            }
            else
            {
                GUI.color = Color.gray;
                Widgets.ButtonText(buttonRect, reason, active: false);
                GUI.color = Color.white;
            }

            TooltipHandler.TipRegion(buttonRect, expertise.description + "\n\n" + "VSE.Effects.PerLevel".Translate() + expertise.Effects(1, "  - "));
            y += 60f;
        }

        Widgets.EndScrollView();

        Text.Font = GameFont.Tiny;
        Widgets.Label(textRect, "VSE.ExpertiseDesc".Translate(SkillsMod.Settings.MaxExpertise).Colorize(ColoredText.SubtleGrayColor));

        Text.Font = font;
        Text.Anchor = anchor;
    }

    public static void DrawSkillsAndExpertiseOf(Pawn pawn, Vector2 offset, SkillUI.SkillDrawMode mode, Rect container)
    {
        Text.Font = GameFont.Small;
        var height = 0f;
        var allDefsListForReading = DefDatabase<SkillDef>.AllDefsListForReading;
        for (var i = 0; i < allDefsListForReading.Count; i++)
        {
            var x = Text.CalcSize(allDefsListForReading[i].skillLabel.CapitalizeFirst()).x;
            if (x > SkillUI.levelLabelWidth) SkillUI.levelLabelWidth = x;
            height += 27f;
        }

        var expertise = pawn.Expertise().AllExpertise.OrderByDescending(ex => ex.def.skill.listOrder).ToList();
        for (var i = 0; i < expertise.Count; i++)
        {
            var x = Text.CalcSize(expertise[i].def.LabelCap).x;
            if (x > SkillUI.levelLabelWidth) SkillUI.levelLabelWidth = x;
            height += 27f;
        }

        var viewRect = new Rect(0, 0, 230f, height);
        Widgets.BeginScrollView(new Rect(offset.x, offset.y, 250f, container.height), ref skillsScrollPos, viewRect);
        var k = 0;
        var y = 0f;
        for (var j = 0; j < SkillUI.skillDefsInListOrderCached.Count; j++)
        {
            var skillDef = SkillUI.skillDefsInListOrderCached[j];
            SkillUI.DrawSkill(pawn.skills.GetSkill(skillDef), new Vector2(0, y), mode);
            y += 27f;
            while (k < expertise.Count && expertise[k].def.skill == skillDef)
            {
                DrawExpertise(expertise[k], new Rect(0, y, 230f, 24f), mode);
                k++;
                y += 27f;
            }
        }

        Widgets.EndScrollView();
    }

    public static void DrawExpertise(ExpertiseRecord expertise, Rect holdingRect, SkillUI.SkillDrawMode mode, string tooltipPrefix = "")
    {
        if (Mouse.IsOver(holdingRect)) GUI.DrawTexture(holdingRect, TexUI.HighlightTex);

        GUI.BeginGroup(holdingRect);
        Text.Anchor = TextAnchor.MiddleLeft;
        var rect = new Rect(6f, 0f, SkillUI.levelLabelWidth + 6f, holdingRect.height);
        Widgets.Label(rect, expertise.def.LabelCap);
        var position = new Rect(rect.xMax, 0f, 24f, 24f);
        GUI.DrawTexture(position, ExpertisePassion);

        Widgets.FillableBar(new Rect(position.xMax, 0f, holdingRect.width - position.xMax, holdingRect.height), Mathf.Max(0.01f, expertise.Level / 20f),
            ExpertiseBarFillTex, null, false);

        var rect3 = new Rect(position.xMax + 4f, 0f, 999f, holdingRect.height);
        rect3.yMin += 3f;
        GenUI.SetLabelAlign(TextAnchor.MiddleLeft);
        Widgets.Label(rect3, expertise.Level.ToStringCached());
        GenUI.ResetLabelAlign();
        GUI.color = Color.white;
        GUI.EndGroup();
        if (Mouse.IsOver(holdingRect))
        {
            var text = expertise.FullDescription();
            if (tooltipPrefix != "") text = tooltipPrefix + "\n\n" + text;

            TooltipHandler.TipRegion(holdingRect, text);
        }
    }
}
