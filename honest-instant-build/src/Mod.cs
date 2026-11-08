using HarmonyLib;

namespace HonestInstantBuild
{
    /// <summary>
    /// Mod entry point. The game finds the one <see cref="KMod.UserMod2"/> subclass in the
    /// assembly and calls <see cref="OnLoad"/>; the base implementation applies every
    /// <c>[HarmonyPatch]</c> class in this assembly.
    /// </summary>
    public sealed class Mod : KMod.UserMod2
    {
        /// <summary>Prefix on every line this mod writes to the game log.</summary>
        public const string LogPrefix = "[HonestInstantBuild]";

        /// <summary>Apply the patches and record which game build we loaded under.</summary>
        /// <param name="harmony">The Harmony instance the game created for this mod's staticID.</param>
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            Debug.Log($"{LogPrefix} v{typeof(Mod).Assembly.GetName().Version} loaded on game changelist {KleiVersion.ChangeList}.");
        }
    }
}
