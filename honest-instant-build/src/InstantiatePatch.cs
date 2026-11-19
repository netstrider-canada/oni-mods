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
            /// <summary>True when the order is finished after Honest Instant Dig digs its cells.</summary>
            public bool ViaDig;
            /// <summary>The cells the game digs for this order (empty when none).</summary>
            public List<int> DigCells;
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

                // Nothing a duplicant could not do: an asteroid with no living duplicant on it (nor in a
                // rocket landed there) keeps the whole order a normal one, nothing paid, nothing dug
                // (decided 2026-11-19, after a play check dug the surface of an empty asteroid).
                if (!DigRules.AnyDuplicantOn(Grid.WorldIdx[cell]))
                {
                    Debug.Log($"{Mod.LogPrefix} {__instance.PrefabID} at cell {cell}: left as a normal order: no living duplicant on this asteroid.");
                    __result.GetComponent<KPrefabID>()?.AddTag(DeferredFinish.LeftNormalTag);
                    return;
                }

                // A building the game lets only a skilled duplicant build (the conveyor family needs the
                // Mechatronics Engineer's ConveyorBuild, Steam Turbines and Heavi-Watt wires the Power
                // Technician's CanPowerTinker; Constructable.requiredSkillPerk, enforced on the Build chore
                // by WorkChore) is instant only when a living duplicant on this asteroid holds that skill.
                // Otherwise the whole order stays a normal one: nothing paid, nothing dug (v0.2.0, decided 2026-11-18,
                // the same rule as digging).
                Constructable order = __result.GetComponent<Constructable>();
                string perk = order != null ? order.requiredSkillPerk : null;
                if (!string.IsNullOrEmpty(perk) && !MinionResume.AnyMinionHasPerk(perk, Grid.WorldIdx[cell]))
                {
                    Debug.Log($"{Mod.LogPrefix} {__instance.PrefabID} at cell {cell}: left as a normal order: needs {perk} on this asteroid.");
                    __result.GetComponent<KPrefabID>()?.AddTag(DeferredFinish.LeftNormalTag);
                    return;
                }

                // Decision 3A: digging stays a duplicant job. An order over solid ground waits
                // for its dig and then proceeds as a normal build - unless Honest Instant Dig is
                // loaded too (v0.2), which digs it at once; then the order is paid now and finished
                // the moment its digs are done (DeferredFinish).
                List<int> digCells = DigCells(__instance, __result.GetComponent<Constructable>(), cell, orientation);
                bool viaDig = false;
                if (digCells == null || digCells.Count > 0)
                {
                    if (digCells == null || !DeferredFinish.DigModLoaded())
                    {
                        Debug.Log($"{Mod.LogPrefix} {__instance.PrefabID} at cell {cell}: skipped, needs digging first.");
                        return;
                    }
                    // Decision 3-i A: every cell must be one an honest duplicant on this asteroid
                    // could dig now, or the whole order stays a normal duplicant order.
                    foreach (int c in digCells)
                    {
                        string reason = DeferredFinish.WhyNotInstantlyDiggable(c, __instance);
                        if (reason != null)
                        {
                            Debug.Log($"{Mod.LogPrefix} {__instance.PrefabID} at cell {cell}: left as a normal order: cell {c} {reason}.");
                            // Tell Honest Instant Dig, by a plain tag (no reference either way), to
                            // leave this order's digs to duplicants too: nothing paid, nothing dug.
                            __result.GetComponent<KPrefabID>()?.AddTag(DeferredFinish.LeftNormalTag);
                            return;
                        }
                    }
                    viaDig = true;
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
                    ViaDig = viaDig,
                    DigCells = digCells,
                };
                // An order this mod will refuse must not have its ground dug first by Honest Instant Dig.
                if (viaDig && !pending.Affordable) __result.GetComponent<KPrefabID>()?.AddTag(DeferredFinish.LeftNormalTag);
                UIScheduler.Instance.Schedule("HonestInstantBuild", 0f, Act, pending);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{Mod.LogPrefix} postfix failed, leaving the order to duplicants: {ex}");
            }
        }

        /// <summary>
        /// The order's cells the game will dig - the same rule as Constructable.PlaceDiggables and
        /// IsCellDigRequired. A tile swap never digs, and a cell that is solid only because a built
        /// tile is there (Grid.Foundation) is not a dig: that is how wires and pipes run through tiles
        /// and how one tile replaces another.
        /// </summary>
        /// <param name="def">The building being ordered.</param>
        /// <param name="constructable">The new order.</param>
        /// <param name="cell">The order's anchor cell.</param>
        /// <param name="orientation">Rotation chosen by the player.</param>
        /// <returns>The dig cells (empty when the order needs no dig), or null when the order needs a
        /// dig that cannot be listed (no Constructable, or a footprint cell off the map).</returns>
        private static List<int> DigCells(BuildingDef def, Constructable constructable, int cell, Orientation orientation)
        {
            var cells = new List<int>();
            if (constructable == null) return null;
            if (constructable.IsReplacementTile) return cells;
            foreach (CellOffset offset in def.PlacementOffsets)
            {
                CellOffset rotated = Rotatable.GetRotatedCellOffset(offset, orientation);
                if (!Grid.IsCellOffsetValid(cell, rotated)) return null;
                int c = Grid.OffsetCell(cell, rotated);
                if ((def.ObjectLayer == ObjectLayer.Backwall && BackwallManager.HasBackwall(c)) || Diggable.IsDiggable(c))
                    cells.Add(c);
            }
            return cells;
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

                if (p.ViaDig)
                {
                    // Paid in full: hand the order to the game's own delivery-complete path, which
                    // places its digs and clears the reservation; DeferredFinish builds it once the
                    // digs are done.
                    DeferredFinish.PayAndWait(constructable, p.Def, p.Cell, report, p.DigCells);
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
                ScheduleGhostSweep(p.Def, p.Cell, p.Order);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{Mod.LogPrefix} finishing {p.Def?.PrefabID} at cell {p.Cell} failed, leaving the order to duplicants: {ex}");
            }
        }

        /// <summary>
        /// After a finish, start the sweep that erases a stale blueprint picture, for the tiles that
        /// draw one (kAnim tiles with a block atlas).
        /// </summary>
        /// <param name="def">The building just finished.</param>
        /// <param name="cell">Its anchor cell.</param>
        /// <param name="order">The order just finished.</param>
        internal static void ScheduleGhostSweep(BuildingDef def, int cell, GameObject order)
        {
            if (def.IsTilePiece && def.isKAnimTile && def.BlockTileAtlas != null)
                UIScheduler.Instance.Schedule("HonestInstantBuild", 0.1f, SweepGhost,
                    new Sweep { Def = def, Cell = cell, Order = order, Until = Time.realtimeSinceStartup + 60f });
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
                bool ours = IsOrder(swap, s.Order) || IsOrder(tile, s.Order);
                // A new order placed on the cell owns its own picture; never erase that one.
                if (!ours && (IsAnyOrder(swap) || IsAnyOrder(tile))) return;
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

        /// <summary>True when the object on a cell's layer is the given order (Unity's null check, so a destroyed object is not).</summary>
        private static bool IsOrder(GameObject onCell, GameObject order) => onCell != null && onCell == order;

        /// <summary>True when the object on a cell's layer is a build order still under construction.</summary>
        private static bool IsAnyOrder(GameObject onCell) => onCell != null && onCell.GetComponent<Constructable>() != null;
    }
}
