using System;
using System.Collections.Generic;
using HarmonyLib;

namespace SelfSealingAirlocks
{
    // Keeps airlock doors sealed in the sim unless the player set them to "Open".
    //
    // Vanilla Door.SetSimState digs out the door's solid cells whenever the door
    // swings open, which lets gas and liquid flow through while a dupe walks past.
    // In Auto (or Locked) mode we keep the cells solid instead; dupes can still
    // walk through because Door.SetPassableState (which runs before this) marks
    // the cells dupe-passable on its own.
    //
    // Everything else (closing, the "Open" control state, temperature,
    // insulation, falling sand) is left to the game's own code, so this patch
    // does not call any SimMessages APIs and survives sim API changes.
    [HarmonyPatch(typeof(Door), "SetSimState")]
    internal static class SelfSealingAirlocks_Door_SetSimState
    {
        private static bool Prefix(Door __instance, ref bool is_door_open, IList<int> cells)
        {
            if (!is_door_open
                || __instance.doorType == Door.DoorType.Internal
                || __instance.CurrentState == Door.ControlState.Opened)
            {
                return true; // vanilla behaviour
            }

            // Door is opening for a dupe while in Auto/Locked mode.
            if (AllCellsHoldDoorSolid(__instance, cells))
            {
                // Already sealed: leave the sim cells alone. Skipping the
                // vanilla open path also skips OnSimDoorOpened, so the door's
                // structure-temperature bypass stays consistent.
                return false;
            }

            // Cells are not solid yet (e.g. the door was switched from Open to
            // Auto while a dupe was standing in it). Run the vanilla "close"
            // path to put the solid door element back.
            is_door_open = false;
            return true;
        }

        internal static bool AllCellsHoldDoorSolid(Door door, IList<int> cells)
        {
            PrimaryElement element = door.GetComponent<PrimaryElement>();
            if (element == null)
            {
                return false;
            }

            for (int i = 0; i < cells.Count; i++)
            {
                int cell = cells[i];
                if (!Grid.IsValidCell(cell) || Grid.Element[cell].id != element.ElementID)
                {
                    return false;
                }
            }
            return true;
        }
    }

    // When the player switches a door to "Open" while it is already swung open
    // (a dupe is in the doorway), the game only refreshes walkability
    // (SetWorldState(updateSim: false)) and never re-runs SetSimState, so the
    // cells we kept solid would stay sealed. Open them here in that case.
    [HarmonyPatch(typeof(Door), "RefreshControlState")]
    internal static class SelfSealingAirlocks_Door_RefreshControlState
    {
        private static readonly Action<Door, bool, IList<int>> SetSimState =
            AccessTools.MethodDelegate<Action<Door, bool, IList<int>>>(
                AccessTools.Method(typeof(Door), "SetSimState"));

        private static void Postfix(Door __instance)
        {
            if (__instance.doorType == Door.DoorType.Internal
                || __instance.CurrentState != Door.ControlState.Opened
                || !__instance.IsOpen())
            {
                return;
            }

            int[] cells = __instance.building.PlacementCells;
            if (SelfSealingAirlocks_Door_SetSimState.AllCellsHoldDoorSolid(__instance, cells))
            {
                SetSimState(__instance, true, cells);
            }
        }
    }

    // Patching Door at mod load runs Door's static initializer early (MonoMod asks
    // for the patched method's address), before the game has loaded its
    // animations. That fills Door.OVERRIDE_ANIMS - the "use the remote" animation a
    // dupe plays while changing a door's mode - with an empty entry for the whole
    // session, so every dupe sent to lock, unlock or open a door throws a
    // NullReferenceException in Worker.StartWork and the errand never finishes.
    // Every door shares that one array, so fill the empty entry back in as each
    // door is set up, once the animations exist. Cause traced first in the Klei
    // forums thread "Simple empty mod causes game to crash" (2023); measured here
    // by the self-test check game-door-errand-animation-loaded (2026-11-08).
    [HarmonyPatch(typeof(Door), "OnPrefabInit")]
    internal static class SelfSealingAirlocks_Door_OnPrefabInit
    {
        // The name Door's own initializer asks for (Door.OVERRIDE_ANIMS, build 744825).
        private const string RemoteAnim = "anim_use_remote_kanim";

        private static void Postfix(Door __instance)
        {
            KAnimFile[] anims = __instance.overrideAnims;
            if (anims == null)
            {
                return;
            }

            for (int i = 0; i < anims.Length; i++)
            {
                if (anims[i] == null)
                {
                    anims[i] = Assets.GetAnim(RemoteAnim);
                }
            }
        }
    }

    // Older versions of this mod set the GasImpermeable flag on every airlock
    // cell, and that flag is stored in the save. Vanilla never sets or clears
    // it on doors, so clear it when a door loads; otherwise doors set to "Open"
    // (and cells of later-deconstructed doors) would keep blocking gas.
    [HarmonyPatch(typeof(Door), "OnSpawn")]
    internal static class SelfSealingAirlocks_Door_OnSpawn
    {
        private const byte GasImpermeable = 1; // Sim.Cell.Properties.GasImpermeable

        private static void Postfix(Door __instance)
        {
            foreach (int cell in __instance.building.PlacementCells)
            {
                SimMessages.ClearCellProperties(cell, GasImpermeable);
            }
        }
    }
}
