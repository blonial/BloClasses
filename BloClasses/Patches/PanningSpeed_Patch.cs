using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace BloClasses.Patches
{
    [HarmonyPatch(typeof(BlockPan))]
    public static class PanningSpeed_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(BlockPan.OnHeldInteractStep))]
        public static void OnHeldInteractStepPrefix(ref float secondsUsed, EntityAgent byEntity)
        {
            secondsUsed = GetModifiedSecondsUsed(secondsUsed, byEntity);
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(BlockPan.OnHeldInteractStop))]
        public static void OnHeldInteractStopPrefix(ref float secondsUsed, EntityAgent byEntity)
        {
            secondsUsed = GetModifiedSecondsUsed(secondsUsed, byEntity);
        }

        private static float GetModifiedSecondsUsed(float secondsUsed, EntityAgent byEntity)
        {
            float speed = byEntity.Stats.GetBlended("panningSpeed");
            if (speed <= 0 || speed == 1)
            {
                return secondsUsed;
            }

            return secondsUsed * speed;
        }
    }
}
