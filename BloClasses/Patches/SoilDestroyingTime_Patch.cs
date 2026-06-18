using HarmonyLib;
using Vintagestory.API.Common;

namespace BloClasses.Patches
{
    [HarmonyPatch(typeof(Block), nameof(Block.OnGettingBroken))]
    public static class SoilDestroyingTime_Patch
    {
        public static void Postfix(Block __instance, IPlayer player, float remainingResistance, ref float __result)
        {
            if (player?.Entity == null || !IsSoil(__instance))
            {
                return;
            }

            float modifier = player.Entity.Stats.GetBlended("soilDestroyingTime");
            if (modifier <= 0 || modifier == 1)
            {
                return;
            }

            float brokenResistance = remainingResistance - __result;
            __result = remainingResistance - brokenResistance / modifier;
        }

        private static bool IsSoil(Block block)
        {
            return block.Code?.Domain == "game" && block.Code.Path.StartsWith("soil-");
        }
    }
}
