using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace HonestInstantBuild
{
    /// <summary>
    /// Moves the recipe's materials from the asteroid's piles into the order's own storage -
    /// the same storage a duplicant delivers into - so the game's normal finish consumes them.
    /// </summary>
    internal static class MaterialPull
    {
        /// <summary>Constructable keeps its delivery storage private; read it the Harmony way.</summary>
        private static readonly AccessTools.FieldRef<Constructable, Storage> StorageOf =
            AccessTools.FieldRefAccess<Constructable, Storage>("storage");

        /// <summary>
        /// Pull every ingredient of the order into its storage, nearest piles first.
        /// </summary>
        /// <param name="constructable">The under-construction object.</param>
        /// <param name="def">Its building definition (for the recipe).</param>
        /// <param name="world">The asteroid the order sits on.</param>
        /// <param name="cell">The order's cell, for distance ordering.</param>
        /// <param name="elements">Materials the player picked.</param>
        /// <returns>
        /// A short human-readable list of what was taken ("100 kg SandStone from 2 piles"),
        /// or null when an ingredient could not be covered - the order is then left to duplicants
        /// with whatever was delivered.
        /// </returns>
        public static string PullInto(Constructable constructable, BuildingDef def, WorldContainer world, int cell, IList<Tag> elements)
        {
            Storage storage = StorageOf(constructable);
            if (storage == null) return null;

            var report = new StringBuilder();
            foreach (Recipe.Ingredient ingredient in def.CraftRecipe.GetAllIngredients(elements))
            {
                float need = ingredient.amount;
                float have = 0f;
                int piles = 0;

                // Same rules as the game's own count: nothing a duplicant is carrying
                // (StoredPrivate), and only this asteroid plus its attached worlds (a docked
                // rocket interior), which is what includeRelatedWorlds means.
                int parent = world.ParentWorldId;
                List<Pickupable> candidates = world.worldInventory
                    .GetPickupables(ingredient.tag, includeRelatedWorlds: true)
                    ?.Where(p => p != null && p.gameObject != null
                                 && !p.KPrefabID.HasTag(GameTags.StoredPrivate)
                                 && SameAsteroid(p, world.id, parent))
                    .OrderBy(p => Grid.GetCellDistance(cell, Grid.PosToCell(p)))
                    .ToList() ?? new List<Pickupable>();

                // First pass: mass nobody has promised to another order. Second pass: the rest.
                foreach (bool unreservedOnly in new[] { true, false })
                {
                    foreach (Pickupable p in candidates)
                    {
                        if (have >= need) break;
                        if (p == null || p.gameObject == null) continue;
                        float available = unreservedOnly ? p.UnreservedAmount : p.TotalAmount;
                        if (available <= 0f) continue;
                        float take = Mathf.Min(available, need - have);
                        Pickupable taken = p.Take(take);
                        if (taken == null) continue;
                        float mass = taken.TotalAmount;
                        storage.Store(taken.gameObject, hide_popups: true);
                        have += mass;
                        piles++;
                    }
                    if (have >= need) break;
                }

                if (have + 0.001f < need)
                {
                    Debug.Log($"{Mod.LogPrefix} {def.PrefabID}: needed {need:0.#} kg {ingredient.tag} but only {have:0.#} kg could be pulled.");
                    return null;
                }
                if (report.Length > 0) report.Append(", ");
                report.Append($"{have:0.#} kg {ingredient.tag} from {piles} pile(s)");
            }
            return report.ToString();
        }

        /// <summary>True when the pile is on this asteroid or one attached to the same parent.</summary>
        private static bool SameAsteroid(Pickupable p, int worldId, int parentWorldId)
        {
            int id = p.GetMyWorldId();
            if (id == worldId) return true;
            WorldContainer w = ClusterManager.Instance.GetWorld(id);
            return w != null && w.ParentWorldId == parentWorldId;
        }
    }
}
