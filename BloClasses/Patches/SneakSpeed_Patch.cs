using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace BloClasses.Patches
{
    [HarmonyPatch(typeof(EntityAgent), nameof(EntityAgent.GetWalkSpeedMultiplier))]
    public static class SneakSpeed_Patch
    {
        [HarmonyPostfix]
        public static void GetWalkSpeedMultiplierPostfix(EntityAgent __instance, ref double __result)
        {
            if (!IsSneaking(__instance))
            {
                return;
            }

            double modifier = __instance.Stats.GetBlended("sneakSpeed");
            if (modifier == 0)
            {
                return;
            }

            __result *= GameMath.Max(0, 1 + modifier);
        }

        private static bool IsSneaking(EntityAgent entity)
        {
            return entity.Controls?.Sneak == true || entity.ServerControls?.Sneak == true;
        }
    }
}
