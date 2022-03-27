﻿using System.Collections.Generic;
using RimWorld;
using Verse;

namespace VSE
{
    public static class ExpertiseTrackers
    {
        private static readonly Dictionary<Pawn_SkillTracker, ExpertiseTracker> trackers = new();

        public static ExpertiseTracker Expertise(this Pawn_SkillTracker tracker) =>
            trackers.TryGetValue(tracker, out var expertise) ? expertise : Create(tracker);

        public static ExpertiseTracker Expertise(this Pawn pawn) =>
            trackers.TryGetValue(pawn.skills, out var tracker) ? tracker : Create(pawn.skills);

        public static ExpertiseTracker Create(Pawn_SkillTracker tracker)
        {
            var expertise = new ExpertiseTracker(tracker);
            trackers.Add(tracker, expertise);
            return expertise;
        }

        public static void CreateExpertise(Pawn_SkillTracker __instance)
        {
            Create(__instance);
        }

        public static void SaveExpertise(Pawn_SkillTracker __instance)
        {
            var e = Expertise(__instance);
            Scribe_Deep.Look(ref e, "expertise", __instance);
            trackers[__instance] = e;
        }
    }
}