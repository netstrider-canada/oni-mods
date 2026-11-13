using System;
using System.Collections.Generic;
using System.Globalization;
using HarmonyLib;
using UnityEngine;

namespace HonestInstantDeconstruction
{
    /// <summary>
    /// The single hook. Every Deconstruct order the player gives - the Deconstruct tool's drag
    /// (<c>DeconstructTool.DeconstructCell</c>), a building's own Deconstruct button and a rocket
    /// module's button - raises the same event, which <c>Deconstructable.OnDeconstruct</c> turns
    /// into <c>QueueDeconstruction(userTriggered: true)</c> after the game's own checks (a building
    /// the game forbids is refused before it gets here; a click on a pending order cancels it).
    ///
    /// <c>QueueDeconstruction</c> already holds the game's instant branch: with the debug flag
    /// set it calls <c>OnCompleteWork(null)</c>, the same finish a duplicant's work runs. The
    /// prefix takes that branch for every player-issued order in a normal game, after mirroring
    /// the game's own refusals, so what drops is exactly what a duplicant would have dropped.
    /// Reconstruct orders and orders re-queued as a save loads (<c>userTriggered</c> false) stay
    /// duplicant jobs.
    /// </summary>
    /// <remarks>The argument list is required: <c>Deconstructable</c> also has a private,
    /// argument-less <c>QueueDeconstruction()</c>, and Harmony refuses an ambiguous name.</remarks>
    [HarmonyPatch(typeof(Deconstructable), nameof(Deconstructable.QueueDeconstruction), new[] { typeof(bool) })]
    internal static class DeconstructPatch
    {
        /// <summary>
        /// The game's protected finish, <c>Deconstructable.OnCompleteWork(WorkerBase)</c>, as a
        /// delegate built once. The call is virtual, so a subclass override is still honoured.
        /// <c>Workable.CompleteWork</c> is deliberately not used: the game's own instant branch does
        /// not call it, and it would also raise <c>WorkableComplete</c> and reset the work timer on
        /// an object about to be deleted.
        /// </summary>
        private static readonly Action<Deconstructable, WorkerBase> CompleteNow =
            AccessTools.MethodDelegate<Action<Deconstructable, WorkerBase>>(AccessTools.Method(typeof(Deconstructable), "OnCompleteWork"));

        /// <summary>
        /// Runs before the game queues a deconstruction chore.
        /// </summary>
        /// <param name="__instance">The building (or loose entity) being deconstructed.</param>
        /// <param name="userTriggered">True for an order the player gave; false for a Reconstruct
        /// order or a pending order re-queued as a save loads.</param>
        /// <returns>False when the order was finished here (the game's method is skipped); true to
        /// let the game's method run unchanged.</returns>
        private static bool Prefix(Deconstructable __instance, bool userTriggered)
        {
            string id = "?";
            int cell = -1;
            try
            {
                // Decision 2A: Reconstruct and load-time orders stay duplicant jobs.
                if (!userTriggered) return true;
                // The game's own instant mode is already instant; stay out of its way.
                if (DebugHandler.InstantBuildMode) return true;
                if (__instance == null) return true;
                // Already finished this frame: the Deconstruct tool raises the order once per object
                // layer, and a ladder or pipe sits in two layers (its ObjectLayer and TileLayer) until
                // its deletion lands at the end of the frame. Skip the game's method too, or it would
                // queue a chore on an object being deleted; the drops happened once already.
                if (__instance.HasBeenDestroyed) return false;
                id = __instance.PrefabID().ToString();
                cell = Grid.PosToCell(__instance);
                // Defensive: the caller already refused a forbidden building, but one must never
                // drop materials through us.
                if (!__instance.allowDeconstruction) return true;
                // The game's first refusal: an order is already pending.
                if (__instance.chore != null) return true;
                // The game's second refusal, copied exactly: a tile swap is waiting on this cell.
                // Returning true lets the game's method hit the same test and return.
                BuildingComplete bc = __instance.GetComponent<BuildingComplete>();
                if (bc != null && bc.Def.ReplacementLayer != ObjectLayer.NumLayers
                    && Grid.Objects[Grid.PosToCell(bc), (int)bc.Def.ReplacementLayer] != null)
                {
                    Debug.Log($"{Mod.LogPrefix} {id} at cell {cell}: left to the game, a replacement is pending.");
                    return true;
                }
                // Read what will drop before the finish: the object is gone afterwards.
                string drops = Drops(__instance);
                CompleteNow(__instance, null);
                Debug.Log($"{Mod.LogPrefix} deconstructed {id} at cell {cell}: {drops}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{Mod.LogPrefix} failed on {id} at cell {cell}, leaving the order to duplicants: {ex}");
                return true;
            }
        }

        /// <summary>
        /// What the game's <c>SpawnItemsFromConstruction</c> will drop, as text for the log line:
        /// one entry per recorded construction element and its recipe mass (<c>Building.Def.Mass</c>),
        /// or the object's own <c>PrimaryElement</c> mass for a building-less loose entity - the same
        /// sources the game reads.
        /// </summary>
        /// <param name="d">The object about to be deconstructed.</param>
        /// <returns>For example <c>200 kg SandStone</c>, entries joined by a comma.</returns>
        private static string Drops(Deconstructable d)
        {
            Tag[] elements = d.constructionElements;
            if (elements == null || elements.Length == 0) return "nothing recorded";
            Building building = d.GetComponent<Building>();
            float[] masses = building != null ? building.Def.Mass : new[] { d.GetComponent<PrimaryElement>().Mass };
            var parts = new List<string>();
            for (int i = 0; i < elements.Length && i < masses.Length; i++)
                parts.Add(string.Format(CultureInfo.InvariantCulture, "{0:0.##} kg {1}", masses[i], elements[i].Name));
            return parts.Count == 0 ? "nothing" : string.Join(", ", parts);
        }
    }
}
