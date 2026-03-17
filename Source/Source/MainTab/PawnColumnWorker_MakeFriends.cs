using Hospitality.Utilities;
using RimWorld;
using Verse;

namespace Hospitality.MainTab
{
    public class PawnColumnWorker_MakeFriends : PawnColumnWorker_Checkbox
    {
        public override bool HasCheckbox(Pawn pawn) => pawn.IsGuest();

        public override bool GetValue(Pawn pawn) => pawn.MakeFriends();

        public override void SetValue(Pawn pawn, bool value, PawnTable table)
        {
            if (table.SortingBy == def)
            {
                table.SetDirty();
            }

            // MP: route guest-table make-friends changes through a synced wrapper instead of a local-only field write
            Multiplayer.SetGuestMakeFriends(pawn, value);
        }
    }
}