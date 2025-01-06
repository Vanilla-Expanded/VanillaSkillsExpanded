using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace VSE
{
    [StaticConstructorOnStartup]
    public static class SettingsHelper
    {
        public static Rect LabelPlus(this Listing_Standard ls, string label, string tooltip = null)
        {
            float num = Text.CalcHeight(label, ls.ColumnWidth);

            Rect rect = ls.GetRect(num);

            Widgets.Label(rect, label);

            if (tooltip != null)
            {
                TooltipHandler.TipRegion(rect, tooltip);
            }
           
            return rect;
        }


    }
}
