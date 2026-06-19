using BloClasses.EntityBehaviors;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace BloClasses.Patches
{
    [HarmonyPatch(typeof(EntityAgent), nameof(EntityAgent.GetWalkSpeedMultiplier))]
    public static class WalkingStickWalkSpeed_Patch
    {
        private const double WalkSpeedMultiplier = 1.1;

        [HarmonyPostfix]
        public static void GetWalkSpeedMultiplierPostfix(EntityAgent __instance, ref double __result)
        {
            if (__instance is not EntityPlayer player || IsSprinting(__instance))
            {
                return;
            }

            if (WalkingStickHeldBonusUtil.PlayerHoldsCowWalkingStick(player))
            {
                __result *= WalkSpeedMultiplier;
            }
        }

        private static bool IsSprinting(EntityAgent entity)
        {
            return entity.Controls?.Sprint == true || entity.ServerControls?.Sprint == true;
        }
    }
}
