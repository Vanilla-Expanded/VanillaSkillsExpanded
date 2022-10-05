using Verse;

namespace VSE;

public static class DebugActionsSkills
{
    [DebugAction("Pawns", "Reset Expertise", true, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static void ResetExpertise(Pawn p)
    {
        p.Expertise()?.ClearExpertise();
    }
}