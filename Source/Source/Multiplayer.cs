using HarmonyLib;
using Hospitality.Utilities;
using Multiplayer.API;
using Verse;
using Verse.AI.Group;

namespace Hospitality;

[StaticConstructorOnStartup]
// MP compatibility entry point.
// Prefer explicit sync wrappers for player-triggered state changes.
// Keep gameplay behavior unchanged unless sync safety requires otherwise.
internal static class Multiplayer
{
    internal static readonly ISyncField[] guestFields;
    internal static readonly ISyncField[] mapFields;

    internal static bool IsRunning => MP.IsInMultiplayer;

    static Multiplayer()
    {
        if (!MP.enabled) return;

        // MP: sync guest state fields that can be changed through Hospitality UI/actions
        guestFields =
        [
            MP.RegisterSyncField(typeof(CompGuest), "guestArea_int").SetBufferChanges(),
            MP.RegisterSyncField(typeof(CompGuest), "shoppingArea_int").SetBufferChanges(),
            MP.RegisterSyncField(typeof(CompGuest), nameof(CompGuest.makeFriends)),
            MP.RegisterSyncField(typeof(CompGuest), nameof(CompGuest.entertain)),
            MP.RegisterSyncField(typeof(CompGuest), nameof(CompGuest.sentAway)),
            MP.RegisterSyncField(typeof(CompGuest), nameof(CompGuest.rescued)),
            MP.RegisterSyncField(typeof(CompGuest), nameof(CompGuest.wasDowned))
        ];

        // MP: sync map-level Hospitality defaults/settings that can be edited in dialogs
        mapFields =
        [
            MP.RegisterSyncField(typeof(Hospitality_MapComponent), nameof(Hospitality_MapComponent.defaultMakeFriends)),
            MP.RegisterSyncField(typeof(Hospitality_MapComponent), nameof(Hospitality_MapComponent.defaultEntertain)),
            MP.RegisterSyncField(typeof(Hospitality_MapComponent), nameof(Hospitality_MapComponent.guestsAreWelcome)),
            MP.RegisterSyncField(typeof(Hospitality_MapComponent), nameof(Hospitality_MapComponent.askForSafety)),
            MP.RegisterSyncField(typeof(Hospitality_MapComponent), nameof(Hospitality_MapComponent.defaultAreaRestriction)).SetBufferChanges(),
            MP.RegisterSyncField(typeof(Hospitality_MapComponent), nameof(Hospitality_MapComponent.defaultAreaShopping)).SetBufferChanges(),
            MP.RegisterSyncField(typeof(Hospitality_MapComponent), nameof(Hospitality_MapComponent.refuseGuestsUntilWeHaveBeds)),
            MP.RegisterSyncField(typeof(Hospitality_MapComponent), nameof(Hospitality_MapComponent.guestsCanTakeFoodForFree)),
            MP.RegisterSyncField(typeof(Hospitality_MapComponent), nameof(Hospitality_MapComponent.drugPolicy))
        ];

        // MP: sync guest ITab actions triggered by players
        MP.RegisterSyncMethod(AccessTools.Method(typeof(ITab_Pawn_Guest), nameof(ITab_Pawn_Guest.SendHome)));
        MP.RegisterSyncMethod(AccessTools.Method(typeof(GuestUtility), nameof(GuestUtility.Recruit)));
        MP.RegisterSyncMethod(AccessTools.Method(typeof(ITab_Pawn_Guest), nameof(ITab_Pawn_Guest.SetAllDefaults)));

        // MP: sync guest bed actions triggered by players
        MP.RegisterSyncMethod(AccessTools.Method(typeof(Building_GuestBed), nameof(Building_GuestBed.Swap)));
        MP.RegisterSyncMethod(AccessTools.Method(typeof(Building_GuestBed), nameof(Building_GuestBed.SetRentalFee)));

        // MP: sync vending-machine actions, including empty-threshold changes
        MP.RegisterSyncMethod(AccessTools.Method(typeof(CompVendingMachine), nameof(CompVendingMachine.SetPrice)));
        MP.RegisterSyncMethod(AccessTools.Method(typeof(CompVendingMachine), nameof(CompVendingMachine.SetEmptyThreshold)));
        MP.RegisterSyncMethod(AccessTools.Method(typeof(CompVendingMachine), nameof(CompVendingMachine.ToggleActive)));

        // MP: sync wrappers for guest-table checkbox changes instead of local-only field writes
        MP.RegisterSyncMethod(AccessTools.Method(typeof(Multiplayer), nameof(SetGuestEntertain)));
        MP.RegisterSyncMethod(AccessTools.Method(typeof(Multiplayer), nameof(SetGuestMakeFriends)));

        // MP: sync guest area changes from the main Hospitality table using stable area IDs
        MP.RegisterSyncMethod(AccessTools.Method(typeof(Multiplayer), nameof(SetGuestAccommodationAreaById)));
        MP.RegisterSyncMethod(AccessTools.Method(typeof(Multiplayer), nameof(SetGuestShoppingAreaById)));
        MP.RegisterSyncMethod(AccessTools.Method(typeof(Multiplayer), nameof(SetLordAccommodationAreaById)));
        MP.RegisterSyncMethod(AccessTools.Method(typeof(Multiplayer), nameof(SetLordShoppingAreaById)));
    }

    // MP sync wrapper: route guest-table entertain toggles through a synced method
    internal static void SetGuestEntertain(Pawn pawn, bool value)
    {
        var compGuest = pawn.CompGuest();
        if (compGuest != null)
            compGuest.entertain = value;
    }

    // MP sync wrapper: route guest-table make-friends toggles through a synced method
    internal static void SetGuestMakeFriends(Pawn pawn, bool value)
    {
        var compGuest = pawn.CompGuest();
        if (compGuest != null)
            compGuest.makeFriends = value;
    }
    // MP sync wrapper: route accommodation-area changes through a synced method using a stable area ID
    internal static void SetGuestAccommodationAreaById(Pawn pawn, int areaId)
    {
        var compGuest = pawn.CompGuest();
        if (compGuest != null)
            compGuest.GuestArea = GetAreaById(pawn.Map, areaId);
    }

    // MP sync wrapper: route shopping-area changes through a synced method using a stable area ID
    internal static void SetGuestShoppingAreaById(Pawn pawn, int areaId)
    {
        var compGuest = pawn.CompGuest();
        if (compGuest != null)
            compGuest.ShoppingArea = GetAreaById(pawn.Map, areaId);
    }

    internal static void SetLordAccommodationAreaById(Pawn pawn, int areaId)
    {
        var lord = pawn?.GetLord();
        if (lord == null)
            return;

        var area = GetAreaById(pawn.Map, areaId);
        foreach (var lordPawn in lord.ownedPawns)
        {
            var compGuest = lordPawn.CompGuest();
            if (compGuest != null)
                compGuest.GuestArea = area;
        }
    }

    internal static void SetLordShoppingAreaById(Pawn pawn, int areaId)
    {
        var lord = pawn?.GetLord();
        if (lord == null)
            return;

        var area = GetAreaById(pawn.Map, areaId);
        foreach (var lordPawn in lord.ownedPawns)
        {
            var compGuest = lordPawn.CompGuest();
            if (compGuest != null)
                compGuest.ShoppingArea = area;
        }
    }

    // MP helper: resolve a synced area ID back to a map area; -1 means no restriction / null
    internal static Area GetAreaById(Map map, int areaId)
    {
        if (map == null || areaId < 0)
            return null;

        foreach (var area in map.areaManager.AllAreas)
        {
            if (area.ID == areaId)
                return area;
        }

        return null;
    }
    // MP: begin watching UI-driven field edits that happen during window drawing
    internal static void WatchBegin()
    {
        MP.WatchBegin();
    }

    // MP: end watched UI mutation scope
    internal static void WatchEnd()
    {
        MP.WatchEnd();
    }

    // MP: watch the provided fields on a specific object while a watched UI block is open
    internal static void Watch(this ISyncField[] fields, object obj)
    {
        if (!MP.IsInMultiplayer) return;

        foreach (var field in fields)
        {
            field.Watch(obj);
        }
    }
}
