using System;
using System.Collections.Generic;
using System.Linq;
using Database;
using HarmonyLib;
using UnityEngine;

namespace HonestInstantBuild
{
    /// <summary>
    /// v0.2: orders over natural ground, finished after Honest Instant Dig digs them. Used only when
    /// that mod is loaded; this assembly never references it (a design rule: neither mod may
    /// break when the other is absent), so alone this mod behaves exactly as v0.1.5.
    ///
    /// The order is paid at once like any other, then handed to the game's own delivery-complete
    /// path (<c>Constructable.OnFetchListComplete</c>), which places its dig markers and clears its
    /// reservation. Honest Instant Dig digs them; the game then creates the Build chore in
    /// <c>Constructable.PlaceDiggables</c>, and the postfix here finishes the order the game's own
    /// way (<c>CompleteWork</c>) instead of waiting for a builder.
    /// </summary>
    internal static class DeferredFinish
    {
        /// <summary>The other mod's assembly name (its staticID).</summary>
        private const string DigModAssembly = "HonestInstantDig";

        /// <summary>Set once the dig mod has been seen; all mods load before any game starts, so a true never turns false.</summary>
        private static bool digModSeen;

        /// <summary>
        /// Put on an order this mod left as a normal duplicant order although Honest Instant Dig is
        /// loaded (Decision 3-i A). Honest Instant Dig reads the same tag name and leaves the order's
        /// digs alone. Not saved: a loaded order's digs are duplicant jobs anyway.
        /// </summary>
        internal static readonly Tag LeftNormalTag = new Tag("HonestInstantBuild_LeftNormal");

        /// <summary>Paid orders waiting for their digs.</summary>
        internal static readonly HashSet<Constructable> Deferred = new HashSet<Constructable>();

        /// <summary>Constructable's private fetch list; null before OnSpawn and after delivery completes.</summary>
        private static readonly AccessTools.FieldRef<Constructable, FetchList2> FetchListOf =
            AccessTools.FieldRefAccess<Constructable, FetchList2>("fetchList");

        /// <summary>Constructable's private Build chore, created by PlaceDiggables once digs and delivery are done.</summary>
        internal static readonly AccessTools.FieldRef<Constructable, Chore> BuildChoreOf =
            AccessTools.FieldRefAccess<Constructable, Chore>("buildChore");

        /// <summary>Constructable's private delivery-complete handler: drops the fetch list, places the digs, clears the reservation.</summary>
        private static readonly Action<Constructable> OnFetchListComplete =
            AccessTools.MethodDelegate<Action<Constructable>>(AccessTools.Method(typeof(Constructable), "OnFetchListComplete"));

        /// <summary>Constructable's private PlaceDiggables: places the digs and, once they are done, the Build chore.</summary>
        private static readonly Action<Constructable> PlaceDiggables =
            AccessTools.MethodDelegate<Action<Constructable>>(AccessTools.Method(typeof(Constructable), "PlaceDiggables"));

        /// <summary>What the finish needs to report.</summary>
        private sealed class Waiting
        {
            public Constructable Order;
            public BuildingDef Def;
            public int Cell;
            public List<int> DigCells;
        }

        /// <summary>The order's details, by order, for the finish and the one-second check.</summary>
        private static readonly Dictionary<Constructable, Waiting> Details = new Dictionary<Constructable, Waiting>();

        /// <summary>True when Honest Instant Dig's assembly is loaded. Checked at decision time, never a reference.</summary>
        internal static bool DigModLoaded()
        {
            if (digModSeen) return true;
            digModSeen = AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == DigModAssembly);
            return digModSeen;
        }

        /// <summary>
        /// Why a dig cell is not one an honest duplicant could dig right now (Decision 3-i A), or null
        /// when it is: the ground is there (not sand still falling), it is not Neutronium, and a living
        /// duplicant on this asteroid holds the digging skill the game demands for it.
        /// </summary>
        /// <param name="cell">A cell the order will dig.</param>
        /// <param name="def">The building being ordered (a backwall order also digs the cell's backwall).</param>
        /// <returns>A short reason for the log line, or null.</returns>
        internal static string WhyNotInstantlyDiggable(int cell, BuildingDef def)
        {
            var elements = new List<Element>();
            if (Grid.Solid[cell] && !Grid.Foundation[cell]) elements.Add(Grid.Element[cell]);
            if (def.ObjectLayer == ObjectLayer.Backwall && BackwallManager.HasBackwall(cell)) elements.Add(BackwallManager.At(cell).Element);
            if (elements.Count == 0) return "is waiting for falling ground";
            foreach (Element e in elements)
            {
                if (e.hardness == byte.MaxValue) return $"{e.id} cannot be dug";
                SkillPerk perk = DigRules.PerkFor(e.hardness);
                if (perk != null && !MinionResume.AnyMinionHasPerk(perk.Id, Grid.WorldIdx[cell]))
                    return $"{e.id} needs {perk.Id} on this asteroid";
            }
            return null;
        }

        /// <summary>
        /// The order is paid (its materials are in its storage): end its delivery the game's way, so
        /// the game places its digs, and wait for them.
        /// </summary>
        /// <param name="constructable">The order.</param>
        /// <param name="def">Its building definition.</param>
        /// <param name="cell">Its anchor cell.</param>
        /// <param name="report">What was pulled, for the log.</param>
        /// <param name="digCells">The cells the game digs for it.</param>
        internal static void PayAndWait(Constructable constructable, BuildingDef def, int cell, string report, List<int> digCells)
        {
            var w = new Waiting { Order = constructable, Def = def, Cell = cell, DigCells = digCells };
            Deferred.Add(constructable);
            Details[constructable] = w;
            Debug.Log($"{Mod.LogPrefix} paid {def.PrefabID} at cell {cell} using {report}; waiting for the dig at cells {string.Join(",", digCells)}.");
            FetchList2 fl = FetchListOf(constructable);
            if (fl != null)
            {
                // Cancels every fetch order and its chores; what is already in the storage stays.
                fl.Cancel("HonestInstantBuild: paid in full");
                OnFetchListComplete(constructable);
            }
            else
            {
                // Delivery already completed: the game's own events drive PlaceDiggables from here.
                PlaceDiggables(constructable);
            }
            UIScheduler.Instance.Schedule("HonestInstantBuild", 1f, StillWaiting, w);
        }

        /// <summary>Logs an order whose digs took longer than a second (a dig left to a duplicant).</summary>
        private static void StillWaiting(object data)
        {
            var w = (Waiting)data;
            if (w.Order != null && Deferred.Contains(w.Order))
                Debug.Log($"{Mod.LogPrefix} {w.Def.PrefabID} at cell {w.Cell}: still waiting for the dig at cells {string.Join(",", w.DigCells)}.");
        }

        /// <summary>
        /// Called when the game has created the Build chore for a waiting order: finish it one frame
        /// later, outside the game's PlaceDiggables (which may itself be running inside the delivery-
        /// complete handler), the way the direct path finishes.
        /// </summary>
        /// <param name="order">The order the chore belongs to.</param>
        internal static void BuildChoreReady(Constructable order)
        {
            if (!Deferred.Remove(order)) return;
            UIScheduler.Instance.Schedule("HonestInstantBuild", 0f, Finish, order);
        }

        /// <summary>Finishes a waiting order with the game's own finish.</summary>
        private static void Finish(object data)
        {
            var order = (Constructable)data;
            Details.TryGetValue(order, out Waiting w);
            try
            {
                if (order == null) { if (w != null) Details.Remove(w.Order); return; }
                Chore chore = BuildChoreOf(order);
                if (chore == null)
                {
                    // Something changed in the frame (the location became invalid, a new dig): wait again.
                    Deferred.Add(order);
                    return;
                }
                Details.Remove(order);
                chore.Cancel("HonestInstantBuild");
                BuildChoreOf(order) = null;
                order.CompleteWork(null);
                if (w != null)
                {
                    Debug.Log($"{Mod.LogPrefix} built {w.Def.PrefabID} at cell {w.Cell} after its dig.");
                    InstantiatePatch.ScheduleGhostSweep(w.Def, w.Cell, order.gameObject);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{Mod.LogPrefix} finishing a dug order at cell {w?.Cell} failed, leaving it to duplicants: {ex}");
            }
        }

        /// <summary>Forgets an order as it is removed (built, or cancelled by the player).</summary>
        internal static void Forget(Constructable order)
        {
            Deferred.Remove(order);
            Details.Remove(order);
        }
    }

    /// <summary>
    /// The game's digging-skill ladder, copied from <c>Diggable.OnSolidChanged</c> (the same ten lines
    /// as Honest Instant Dig's, which this assembly may not reference). Public so the self-test can pin
    /// both copies to the game's own thresholds (hdg-07).
    /// </summary>
    public static class DigRules
    {
        /// <summary>The skill perk a duplicant needs to dig ground of this hardness, or null when anyone may.</summary>
        /// <param name="hardness">The element's hardness (0 to 255; 255 is Neutronium, never dug).</param>
        /// <returns>The perk the game's dig chore demands for that hardness, or null below 50.</returns>
        public static SkillPerk PerkFor(int hardness)
        {
            if (hardness >= 251) return Db.Get().SkillPerks.CanDigRadioactiveMaterials;
            if (hardness >= 200) return Db.Get().SkillPerks.CanDigSuperDuperHard;
            if (hardness >= 150) return Db.Get().SkillPerks.CanDigNearlyImpenetrable;
            if (hardness >= 50) return Db.Get().SkillPerks.CanDigVeryFirm;
            return null;
        }

        /// <summary>
        /// True when a living duplicant is on this asteroid or in a rocket landed on it: the same
        /// asteroid-and-its-child-worlds reach <c>MinionResume.AnyMinionHasPerk</c> uses.
        /// </summary>
        /// <param name="worldId">The asteroid's world id (<c>Grid.WorldIdx</c> of the cell).</param>
        /// <returns>False when nobody there could do the work.</returns>
        public static bool AnyDuplicantOn(int worldId) =>
            Components.LiveMinionIdentities.GetWorldItems(worldId, true).Count > 0;
    }

    /// <summary>Spots the moment the game creates the Build chore for a waiting order.</summary>
    [HarmonyPatch(typeof(Constructable), "PlaceDiggables")]
    internal static class PlaceDiggablesPatch
    {
        /// <summary>Runs after the game's PlaceDiggables.</summary>
        /// <param name="__instance">The build order.</param>
        private static void Postfix(Constructable __instance)
        {
            try
            {
                if (__instance == null || !DeferredFinish.Deferred.Contains(__instance)) return;
                if (DeferredFinish.BuildChoreOf(__instance) != null)
                    DeferredFinish.BuildChoreReady(__instance);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{Mod.LogPrefix} watching a dug order failed: {ex}");
            }
        }
    }

    /// <summary>Forgets an order as it is removed.</summary>
    [HarmonyPatch(typeof(Constructable), "OnCleanUp")]
    internal static class ConstructableCleanUpPatch
    {
        /// <summary>Runs after the game's OnCleanUp.</summary>
        /// <param name="__instance">The order being removed.</param>
        private static void Postfix(Constructable __instance) => DeferredFinish.Forget(__instance);
    }
}
