using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace HonestInstantBuild
{
    /// <summary>
    /// The single hook. <c>BuildingDef.Instantiate</c> is the factory every build tool uses to
    /// create an under-construction object (buildings via TryPlace, tile swaps via
    /// TryReplaceTile, the wire/pipe tool's replacement branch). It is not used when a save
    /// loads, and the game's own sandbox/debug instant build bypasses it entirely.
    ///
    /// The postfix decides, inline, whether the asteroid can pay for the order - BEFORE the
    /// order's OnSpawn registers its own material need (OnSpawn runs from Unity's Start, next
    /// frame), because MaterialsAvailable subtracts registered needs and would otherwise refuse
    /// when stock exactly covers the order. It then acts one frame later through UIScheduler,
    /// which runs on real time so a build laid out while paused still completes.
    /// </summary>
    [HarmonyPatch(typeof(BuildingDef), nameof(BuildingDef.Instantiate))]
    internal static class InstantiatePatch
    {
        /// <summary>Constructable's private fetch list; null before OnSpawn and after delivery completes.</summary>
        private static readonly AccessTools.FieldRef<Constructable, FetchList2> FetchListOf =
            AccessTools.FieldRefAccess<Constructable, FetchList2>("fetchList");

        /// <summary>Constructable's private, idempotent removal of its own MaterialNeeds entry.</summary>
        private static readonly Action<Constructable> ClearMaterialNeeds =
            AccessTools.MethodDelegate<Action<Constructable>>(AccessTools.Method(typeof(Constructable), "ClearMaterialNeeds"));

        /// <summary>What the postfix decided and what the callback needs to act on it.</summary>
        private sealed class Pending
        {
            public GameObject Order;
            public BuildingDef Def;
            public WorldContainer World;
            public int Cell;
            public IList<Tag> Elements;
            public bool Affordable;
            public int Waits;
        }

        /// <summary>Frames to wait for an order's own OnSpawn before leaving it to duplicants.</summary>
        private const int MaxSpawnWaits = 120;

        /// <summary>
        /// Runs after the game created the under-construction object.
        /// </summary>
        /// <param name="__instance">The building being ordered.</param>
        /// <param name="__result">The under-construction object the game just created.</param>
        /// <param name="pos">World position of the order (already offset in z by the game).</param>
        /// <param name="orientation">Rotation chosen by the player.</param>
        /// <param name="selected_elements">Materials the player picked, primary first.</param>
        private static void Postfix(BuildingDef __instance, GameObject __result, Vector3 pos, Orientation orientation, IList<Tag> selected_elements)
        {
            try
            {
                if (__result == null || __instance == null) return;

                // The game's own instant modes are already free and never reach here, but a
                // mod stacking order could; stay out of their way.
                if (DebugHandler.InstantBuildMode) return;
                if (Game.Instance != null && Game.Instance.SandboxModeActive
                    && SandboxToolParameterMenu.instance != null
                    && SandboxToolParameterMenu.instance.settings.InstantBuild) return;

                int cell = Grid.PosToCell(pos);
                if (!Grid.IsValidCell(cell)) return;

                // Decision 3A: digging stays a duplicant job. An order over solid ground waits
                // for its dig and then proceeds as a normal build.
                if (NeedsDigging(__instance, __result.GetComponent<Constructable>(), cell, orientation))
                {
                    Debug.Log($"{Mod.LogPrefix} {__instance.PrefabID} at cell {cell}: skipped, needs digging first.");
                    return;
                }

                WorldContainer world = ClusterManager.Instance.GetWorld(Grid.WorldIdx[cell]);
                if (world == null || world.worldInventory == null) return;

                var pending = new Pending
                {
                    Order = __result,
                    Def = __instance,
                    World = world,
                    Cell = cell,
                    Elements = new List<Tag>(selected_elements),
                    Affordable = __instance.MaterialsAvailable(selected_elements, world),
                };
                UIScheduler.Instance.Schedule("HonestInstantBuild", 0f, Act, pending);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{Mod.LogPrefix} postfix failed, leaving the order to duplicants: {ex}");
            }
        }

        /// <summary>
        /// True when the game would place a dig for this order - the same rule as
        /// Constructable.PlaceDiggables/IsCellDigRequired. A tile swap never digs, and a cell
        /// that is solid only because a built tile is there (Grid.Foundation) is not a dig:
        /// that is how wires and pipes run through tiles and how one tile replaces another.
        /// </summary>
        /// <param name="def">The building being ordered.</param>
        /// <param name="constructable">The new order (null is treated as needing a dig).</param>
        /// <param name="cell">The order's anchor cell.</param>
        /// <param name="orientation">Rotation chosen by the player.</param>
        private static bool NeedsDigging(BuildingDef def, Constructable constructable, int cell, Orientation orientation)
        {
            if (constructable == null) return true;
            if (constructable.IsReplacementTile) return false;
            foreach (CellOffset offset in def.PlacementOffsets)
            {
                CellOffset rotated = Rotatable.GetRotatedCellOffset(offset, orientation);
                if (!Grid.IsCellOffsetValid(cell, rotated)) return true;
                int c = Grid.OffsetCell(cell, rotated);
                if (def.ObjectLayer == ObjectLayer.Backwall && BackwallManager.HasBackwall(c)) return true;
                if (Diggable.IsDiggable(c)) return true;
            }
            return false;
        }

        /// <summary>
        /// One frame after placement: refuse (cancel the order) or pay and finish it.
        /// </summary>
        private static void Act(object data)
        {
            var p = (Pending)data;
            try
            {
                if (p.Order == null) return; // the player cancelled it within the frame
                var constructable = p.Order.GetComponent<Constructable>();
                if (constructable == null) return;

                // Wait for the order's own OnSpawn. A fast drag places several orders in one
                // frame, and this callback can then run before an order has spawned. Finishing it
                // early let that late OnSpawn draw a tile swap's blueprint ghost after the swap was
                // done, and nothing ever erased it (found in play-testing, 2026-11-11); refusing it
                // early sends a Cancel nobody is subscribed to yet, since OnSpawn subscribes it.
                if (!constructable.isSpawned)
                {
                    if (++p.Waits > MaxSpawnWaits)
                    {
                        Debug.LogWarning($"{Mod.LogPrefix} {p.Def.PrefabID} at cell {p.Cell}: never spawned, left as a normal order.");
                        return;
                    }
                    UIScheduler.Instance.Schedule("HonestInstantBuild", 0f, Act, p);
                    return;
                }
                if (p.Waits > 0) Debug.Log($"{Mod.LogPrefix} {p.Def.PrefabID} at cell {p.Cell}: waited {p.Waits} frame(s) for the order to spawn.");

                if (!p.Affordable)
                {
                    // Decision 2B: refuse. The game already showed its "No materials" pop-up at
                    // placement; the Cancel event is what the Cancel tool sends, so the order's
                    // own Cancellable deletes it and Constructable clears its material need.
                    Debug.Log($"{Mod.LogPrefix} {p.Def.PrefabID} at cell {p.Cell}: refused, not enough material on this asteroid.");
                    p.Order.Trigger((int)GameHashes.Cancel);
                    return;
                }

                string report = MaterialPull.PullInto(constructable, p.Def, p.World, p.Cell, p.Elements);
                if (report == null)
                {
                    // Stock changed in the one frame between check and pull; whatever was
                    // delivered stays in the order and duplicants finish it normally.
                    Debug.Log($"{Mod.LogPrefix} {p.Def.PrefabID} at cell {p.Cell}: stock changed before the pull, left as a normal order.");
                    return;
                }

                // The order's OnSpawn added its recipe to the world's MaterialNeeds ("reserved"),
                // which the game only removes when the fetch list completes or the order is
                // cancelled - neither happens on this path, so without this the reservation
                // leaks: the resource panel (stock minus needs) drops by double the cost and
                // MaterialsAvailable refuses later orders the colony can afford. A non-null
                // fetch list means OnSpawn ran and the reservation is still standing.
                if (FetchListOf(constructable) != null) ClearMaterialNeeds(constructable);

                // The game's own finish: temperature, tile replacement and refund, connections,
                // Build(..., storage, ...), new-building events, and the storage consume that is
                // the deduction. A null worker is what the game itself passes for sandbox uproots.
                constructable.CompleteWork(null);
                Debug.Log($"{Mod.LogPrefix} built {p.Def.PrefabID} at cell {p.Cell} using {report}.");
                if (p.Def.IsTilePiece && p.Def.isKAnimTile && p.Def.BlockTileAtlas != null)
                    UIScheduler.Instance.Schedule("HonestInstantBuild", 0.1f, SweepGhost,
                        new Sweep { Def = p.Def, Cell = p.Cell, Order = p.Order, Until = Time.realtimeSinceStartup + 60f });
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{Mod.LogPrefix} finishing {p.Def?.PrefabID} at cell {p.Cell} failed, leaving the order to duplicants: {ex}");
            }
        }

        /// <summary>More layers of cursor preview than a drag can stack on one cell.</summary>
        private const int MaxGhostLayers = 256;

        /// <summary>The build tool's private record of the cell its cursor preview was last drawn on.</summary>
        private static readonly AccessTools.FieldRef<BuildTool, int> ToolLastCell =
            AccessTools.FieldRefAccess<BuildTool, int>("lastCell");

        /// <summary>A finished tile whose cell may still carry a stale blueprint picture.</summary>
        private sealed class Sweep
        {
            public BuildingDef Def;
            public int Cell;
            public GameObject Order; // the order just finished: it holds the cell until the game's finish lands
            public float Until;
        }

        /// <summary>
        /// Erase a blueprint picture the build tool's cursor preview left on a tile this mod finished.
        /// The tool draws a preview under the cursor and erases it as the cursor leaves, but skips the
        /// erase while a swap order holds the cell; with the game running, a swap the mod finishes can
        /// land in that window, and the picture then stays until a reload (a play-tester's drag, 2026-11-11;
        /// reproduced by the self-test's hib-16). Waits until the swap has fully finished and the cursor
        /// is off the cell, then removes this building's under-construction and swap pictures there.
        /// Only pictures are touched: a built tile is drawn under a different key.
        /// </summary>
        private static void SweepGhost(object data)
        {
            var s = (Sweep)data;
            try
            {
                if (!Grid.IsValidCell(s.Cell)) return;
                GameObject swap = s.Def.ReplacementLayer != ObjectLayer.NumLayers ? Grid.Objects[s.Cell, (int)s.Def.ReplacementLayer] : null;
                GameObject tile = Grid.Objects[s.Cell, (int)s.Def.TileLayer];
                bool ours = (swap != null && swap == s.Order) || (tile != null && tile == s.Order);
                bool otherOrder = !ours && ((swap != null && swap.GetComponent<Constructable>() != null)
                                     || (tile != null && tile.GetComponent<Constructable>() != null));
                // A new order placed on the cell owns its own picture; never erase that one.
                if (otherOrder) return;
                BuildTool tool = BuildTool.Instance;
                bool cursorHere = tool != null && PlayerController.Instance != null
                                  && PlayerController.Instance.ActiveTool == tool && ToolLastCell(tool) == s.Cell;
                if (ours || cursorHere)
                {
                    if (Time.realtimeSinceStartup < s.Until)
                        UIScheduler.Instance.Schedule("HonestInstantBuild", 0.1f, SweepGhost, s);
                    return;
                }
                // The tool adds one layer per frame its preview is redrawn on the cell, so a slow drag
                // on a busy colony stacked five or six (self-test on Spacejunk, 2026-11-12). Removing
                // past the last layer is a no-op, so take off far more than any drag can stack.
                for (int i = 0; i < MaxGhostLayers; i++)
                {
                    global::World.Instance.blockTileRenderer.RemoveBlock(s.Def, true, SimHashes.Void, s.Cell);
                    global::World.Instance.blockTileRenderer.RemoveBlock(s.Def, false, SimHashes.Void, s.Cell);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{Mod.LogPrefix} clearing the blueprint picture at cell {s.Cell} failed: {ex}");
            }
        }
    }
}
