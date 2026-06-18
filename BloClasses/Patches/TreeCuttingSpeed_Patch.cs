using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace BloClasses.Patches
{
    [HarmonyPatch(typeof(Block), nameof(Block.OnGettingBroken))]
    public static class TreeCuttingSpeed_Patch
    {
        public static void Postfix(Block __instance, IPlayer player, float remainingResistance, ref float __result)
        {
            if (player?.Entity == null || !IsTreeLog(__instance))
            {
                return;
            }

            float modifier = player.Entity.Stats.GetBlended("treeCuttingSpeed");
            if (modifier <= 0 || modifier == 1)
            {
                return;
            }

            float brokenResistance = remainingResistance - __result;
            __result = remainingResistance - brokenResistance * modifier;
        }

        private static bool IsTreeLog(Block block)
        {
            return block is BlockLog;
        }
    }
}
