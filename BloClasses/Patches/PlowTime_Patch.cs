using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace BloClasses.Patches
{
    [HarmonyPatch(typeof(ItemHoe))]
    public static class PlowTime_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(ItemHoe.OnHeldInteractStep))]
        public static void OnHeldInteractStepPrefix(ref float secondsUsed, EntityAgent byEntity)
        {
            secondsUsed = GetModifiedSecondsUsed(secondsUsed, byEntity);
        }

        private static float GetModifiedSecondsUsed(float secondsUsed, EntityAgent byEntity)
        {
            float modifier = byEntity.Stats.GetBlended("plowTime");
            if (modifier <= 0 || modifier == 1)
            {
                return secondsUsed;
            }

            return secondsUsed / modifier;
        }
    }
}
