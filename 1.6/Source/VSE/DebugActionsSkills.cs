using LudeonTK;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using VSE.Expertise;

namespace VSE;

public static class DebugActionsSkills
{
    [DebugAction("VE Skills", "Reset Expertise", false, false, false, false, false, 0, false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static void ResetExpertise(Pawn p)
    {
        p.Expertise()?.ClearExpertise();
    }



    [DebugAction("VE Skills", "Add Expertise", false, false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static List<DebugActionNode> AddExpertise()
    {
        List<DebugActionNode> list = new List<DebugActionNode>();

        foreach (ExpertiseDef allDef in DefDatabase<ExpertiseDef>.AllDefs.Where(x => !x.hide))
        {

            list.Add(new DebugActionNode(allDef.LabelCap, DebugActionType.ToolMapForPawns, null, delegate (Pawn p)
            {
                if (p.Expertise()?.AllExpertise.Where(x => x.def == allDef)?.Count() == 0)
                {
                    p.Expertise()?.AddExpertise(allDef);
                }

                DebugActionsUtility.DustPuffFrom(p);
            }));

        }
        return list;
    }

    [DebugAction("VE Skills", "Set Expertise Level", false, false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static List<DebugActionNode> SetExpertise()
    {
        List<DebugActionNode> list = new List<DebugActionNode>();

        foreach (ExpertiseDef allDef in DefDatabase<ExpertiseDef>.AllDefs.Where(x => !x.hide))
        {
            DebugActionNode debugActionNode = new DebugActionNode(allDef.LabelCap);
            for (int i = 0; i <= 20; i++)
            {
                int level = i;

                debugActionNode.AddChild(new DebugActionNode(level.ToString(), DebugActionType.ToolMapForPawns)
                {
                    pawnAction = delegate (Pawn p)
                    {
                        if (p.Expertise()?.AllExpertise.Where(x => x.def == allDef)?.Count() == 0)
                        {
                            p.Expertise()?.AddExpertise(allDef);
                        }
                        p.Expertise().AllExpertise.Where(x => x.def == allDef).FirstOrFallback().Level = level;
                        DebugActionsUtility.DustPuffFrom(p);

                    }
                });
            }


            list.Add(debugActionNode);

        }
        return list;
    }


   
}

