using System;
using System.Collections.Generic;
using System.Globalization;
using Database;
using HarmonyLib;
using UnityEngine;

namespace HonestInstantDig
{
    /// <summary>
    /// What the mod remembers between patches: which dig markers it may act on, which build
    /// orders were placed in the running game, and which digs the sim has been asked for.
    /// Every set holds live objects only; the clean-up patches remove an object as it goes.
    /// </summary>
    internal static class DigState
    {
        /// <summary>Dig markers created in the running game (Decision 4 B): only these are dug at once.</summary>
        public static readonly HashSet<Diggable> Ours = new HashSet<Diggable>();

        /// <summary>Build orders placed in the running game (through <c>BuildingDef.Instantiate</c>, never used by a loading save).</summary>
        public static readonly HashSet<Constructable> NewOrders = new HashSet<Constructable>();

        /// <summary>
        /// Digs the sim has been asked for and has not answered yet, as (cell, backwall). A second
        /// request for the same cell before the answer would dig twice.
        /// </summary>
        public static readonly HashSet<(int cell, bool backwall)> Pending = new HashSet<(int, bool)>();

        /// <summary>The tag Honest Instant Build puts on an order it left as a normal duplicant order (same name in both mods; no reference).</summary>
        public static readonly Tag LeftNormalTag = new Tag("HonestInstantBuild_LeftNormal");

        /// <summary>Markers whose "left to duplicants" line has been logged, so it is logged once per marker.</summary>
        public static readonly HashSet<Diggable> Told = new HashSet<Diggable>();
    }

    /// <summary>
    /// The game's digging-skill ladder, copied from <c>Diggable.OnSolidChanged</c>. Public so the
    /// self-test can pin it to the game's own thresholds (hdg-07) without referencing this assembly.
    /// </summary>
    public static class DigRules
    {
        /// <summary>
        /// The skill perk a duplicant needs to dig ground of this hardness, or null when anyone may.
        /// </summary>
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

    /// <summary>
    /// Marks a Dig-tool marker as ours when <c>DigTool.PlaceDig</c> creates it. PlaceDig returns the
    /// marker already on the cell when there is one; that one is not new and is not marked here.
    /// </summary>
    [HarmonyPatch(typeof(DigTool), nameof(DigTool.PlaceDig))]
    internal static class PlaceDigPatch
    {
        /// <summary>Records whether the cell had no marker before the game's method ran.</summary>
        /// <param name="cell">The cell being given a Dig order.</param>
        /// <param name="__state">True when the cell had no marker.</param>
        private static void Prefix(int cell, out bool __state)
        {
            __state = Grid.IsValidCell(cell) && Grid.Objects[cell, 7] == null;
        }

        /// <summary>Marks the marker the game just created.</summary>
        /// <param name="__result">The marker on the cell after the call, or null when none was placed.</param>
        /// <param name="__state">The prefix's record.</param>
        private static void Postfix(GameObject __result, bool __state)
        {
            if (!__state || __result == null) return;
            Diggable d = __result.GetComponent<Diggable>();
            if (d != null) DigState.Ours.Add(d);
        }
    }

    /// <summary>
    /// Records every build order placed in the running game. <c>BuildingDef.Instantiate</c> is the
    /// factory every build tool uses and is never used when a save loads, so a loaded order is never
    /// recorded (the same fact Honest Instant Build stands on).
    /// </summary>
    [HarmonyPatch(typeof(BuildingDef), nameof(BuildingDef.Instantiate))]
    internal static class InstantiatePatch
    {
        /// <summary>Adds the new order to <see cref="DigState.NewOrders"/>.</summary>
        /// <param name="__result">The under-construction object the game just created.</param>
        private static void Postfix(GameObject __result)
        {
            if (__result == null) return;
            Constructable c = __result.GetComponent<Constructable>();
            if (c != null) DigState.NewOrders.Add(c);
        }
    }

    /// <summary>
    /// Marks the hidden dig markers a new build order places over natural ground. Only markers this
    /// call creates are marked, so a marker a loaded save brought back stays a duplicant job even when
    /// a new order covers it.
    /// </summary>
    [HarmonyPatch(typeof(Constructable), "PlaceDiggables")]
    internal static class PlaceDiggablesPatch
    {
        /// <summary>Lists the order's cells that have no live marker yet (the ones the game may create one on).</summary>
        /// <param name="__instance">The build order.</param>
        /// <param name="__state">Those cells, or null when the order is not one placed in the running game.</param>
        private static void Prefix(Constructable __instance, out List<int> __state)
        {
            __state = null;
            if (__instance == null || !DigState.NewOrders.Contains(__instance)) return;
            // Honest Instant Build (when loaded) tags an order it left to duplicants because one of
            // its cells is ground nobody here can dig yet: then none of its digs is ours either.
            if (__instance.HasTag(DigState.LeftNormalTag)) return;
            Building building = __instance.GetComponent<Building>(); // the order's private 'building' field holds this same component
            if (building == null) return;
            __state = new List<int>();
            foreach (int cell in building.PlacementCells)
            {
                if (!Grid.IsValidCell(cell)) continue;
                Diggable d = Diggable.GetDiggable(cell);
                if (d == null || !d.isActiveAndEnabled) __state.Add(cell);
            }
        }

        /// <summary>Marks the markers the game created on those cells.</summary>
        /// <param name="__state">The prefix's list.</param>
        private static void Postfix(List<int> __state)
        {
            if (__state == null) return;
            foreach (int cell in __state)
            {
                Diggable d = Diggable.GetDiggable(cell);
                if (d != null) DigState.Ours.Add(d);
            }
        }
    }

    /// <summary>Forgets a marker as it is removed.</summary>
    [HarmonyPatch(typeof(Diggable), "OnCleanUp")]
    internal static class DiggableCleanUpPatch
    {
        /// <summary>Drops the marker and its cell's pending digs.</summary>
        /// <param name="__instance">The marker being removed.</param>
        private static void Postfix(Diggable __instance)
        {
            DigState.Ours.Remove(__instance);
            DigState.Told.Remove(__instance);
            int cell = __instance.GetCell();
            DigState.Pending.Remove((cell, false));
            DigState.Pending.Remove((cell, true));
        }
    }

    /// <summary>Forgets a build order as it is removed.</summary>
    [HarmonyPatch(typeof(Constructable), "OnCleanUp")]
    internal static class ConstructableCleanUpPatch
    {
        /// <summary>Drops the order from <see cref="DigState.NewOrders"/>.</summary>
        /// <param name="__instance">The order being removed.</param>
        private static void Postfix(Constructable __instance)
        {
            DigState.NewOrders.Remove(__instance);
        }
    }

    /// <summary>
    /// The acting hook (Decision 1 A). The game calls <c>Diggable.OnSolidChanged</c> from the marker's
    /// <c>OnSpawn</c>, on every solid- or backwall-changed event of its cell, and every two seconds while
    /// unstable ground falls towards it. After the game's own handling, this asks for the dig the way a
    /// duplicant's last work tick does (Decision 2 A), after the game's own refusals and the digging-skill
    /// rule for that asteroid (Decision 3 A).
    /// </summary>
    [HarmonyPatch(typeof(Diggable), "OnSolidChanged")]
    internal static class SolidChangedPatch
    {
        /// <summary>Digs the marker's cell now when an honest duplicant could.</summary>
        /// <param name="__instance">The dig marker.</param>
        private static void Postfix(Diggable __instance)
        {
            int cell = -1;
            (int, bool) key = (-1, false);
            try
            {
                if (DebugHandler.InstantBuildMode) return;
                if (__instance == null || __instance.gameObject == null) return;
                if (!DigState.Ours.Contains(__instance)) return;
                cell = __instance.GetCell();
                if (!Grid.IsValidCell(cell) || Grid.Objects[cell, 7] != __instance.gameObject) return;

                // A phase whose ground is gone has been answered by the sim: forget its request, so
                // sand that later falls into the dug cell can be dug at once too.
                if (!Grid.Solid[cell]) DigState.Pending.Remove((cell, false));
                if (!BackwallManager.HasBackwall(cell)) DigState.Pending.Remove((cell, true));

                bool backwall;
                if (__instance.WillDigTile() && Grid.Solid[cell]) backwall = false;
                else if (__instance.WillDigBackwall() && BackwallManager.HasBackwall(cell)) backwall = true;
                else return; // nothing to dig yet (sand still falling); the game calls again when it lands

                // The other phase finished: the cell changed since it was asked for.
                DigState.Pending.Remove((cell, !backwall));
                key = (cell, backwall);
                if (DigState.Pending.Contains(key)) return;

                Element element = backwall ? BackwallManager.At(cell).Element : Grid.Element[cell];
                if (element.hardness == byte.MaxValue)
                {
                    TellOnce(__instance, $"left to the game: {element.id} at cell {cell} cannot be dug.");
                    return;
                }
                // Nothing a duplicant could not do: an asteroid with no living duplicant on it (nor in a
                // rocket landed there) keeps the order a duplicant job, soft ground too (decided 2026-11-19,
                // after a play check dug the surface of an empty asteroid).
                if (!DigRules.AnyDuplicantOn(Grid.WorldIdx[cell]))
                {
                    TellOnce(__instance, $"left to duplicants: {element.id} at cell {cell}, no living duplicant on this asteroid.");
                    return;
                }
                SkillPerk perk = DigRules.PerkFor(element.hardness);
                if (perk != null && !MinionResume.AnyMinionHasPerk(perk.Id, Grid.WorldIdx[cell]))
                {
                    TellOnce(__instance, $"left to duplicants: {element.id} at cell {cell} needs {perk.Id} on this asteroid.");
                    return;
                }

                float mass = (backwall ? BackwallManager.At(cell).Mass : Grid.Mass[cell]) * 0.5f;
                DigState.Pending.Add(key);
                WorldDamage.Instance.ApplyDamage(cell, 1f, -1, WorldDamage.DamageType.Absolute);
                Debug.Log(string.Format(CultureInfo.InvariantCulture, "{0} dug {1} at cell {2} ({3}): {4:0.##} kg expected",
                    Mod.LogPrefix, element.id, cell, backwall ? "backwall" : "tile", mass));
            }
            catch (Exception ex)
            {
                DigState.Pending.Remove(key);
                Debug.LogWarning($"{Mod.LogPrefix} failed at cell {cell}, leaving the dig to duplicants: {ex}");
            }
        }

        /// <summary>Logs a "left to ..." line once per marker.</summary>
        /// <param name="d">The marker.</param>
        /// <param name="text">The line, without the prefix.</param>
        private static void TellOnce(Diggable d, string text)
        {
            if (DigState.Told.Add(d)) Debug.Log($"{Mod.LogPrefix} {text}");
        }
    }
}
