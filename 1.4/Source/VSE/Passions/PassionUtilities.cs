using System.Linq;
using RimWorld;
using Verse;

namespace VSE.Passions;

public static class PassionUtilities
{
    public static Passion ChangePassion(this Passion passion, int offset)
    {
        var passions = PassionManager.Passions.ToList();
        passions.SortBy(def => def.learnRateFactor);
        var idx = passions.IndexOf(PassionManager.PassionToDef(passion)) + offset;
        if (idx < 0) idx += passions.Count;
        if (idx >= passions.Count) idx -= passions.Count;
        return (Passion)passions[idx].index;
    }
}