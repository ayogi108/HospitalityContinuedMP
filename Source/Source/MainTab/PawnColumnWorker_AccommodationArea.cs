using Hospitality.Utilities;
using UnityEngine;
using Verse;

namespace Hospitality.MainTab;

internal class PawnColumnWorker_AccommodationArea : PawnColumnWorker_AreaBase
{
    protected override Area GetArea(Pawn pawn)
    {
        var comp = pawn.CompGuest();
        return comp?.GuestArea;
    }

    protected override void SetArea(Pawn pawn, Area area)
    {
        // MP: route accommodation-area changes through a synced wrapper using a stable area ID
        Multiplayer.SetGuestAccommodationAreaById(pawn, area?.ID ?? -1);
    }

    protected override void DrawTopArea(Rect rect)
    {
        rect.width -= 10;
        rect.x += 5;
        if (Widgets.ButtonText(rect, "MapSettings".Translate(), true, false))
        {
            Find.WindowStack.Add(new Dialog_MapSettings(Find.CurrentMap));
        }
    }
}