using Hospitality.Utilities;
using RimWorld;
using Verse;

namespace Hospitality.MainTab
{
    public class PawnColumnWorker_Entertain : PawnColumnWorker_Checkbox
    {
        public override bool HasCheckbox(Pawn pawn) => pawn.IsGuest();

        public override bool GetValue(Pawn pawn) => pawn.ImproveRelationship();

        public override void SetValue(Pawn pawn, bool value, PawnTable table)
        {
            if (table.SortingBy == def)
            {
                table.SetDirty();
            }

            // MP: route guest-table entertain changes through a synced wrapper instead of a local-only field write
            Multiplayer.SetGuestEntertain(pawn, value);
        }
    }
}