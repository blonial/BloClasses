using HarmonyLib;
using Vintagestory.API.Common;

namespace BloClasses.Patches
{
    [HarmonyPatch(typeof(CollectibleObject))]
    public static class AttackSpeed_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(CollectibleObject.OnHeldAttackStep))]
        public static void OnHeldAttackStepPrefix(ref float secondsPassed, EntityAgent byEntity)
        {
            secondsPassed = GetModifiedSecondsPassed(secondsPassed, byEntity);
        }

        private static float GetModifiedSecondsPassed(float secondsPassed, EntityAgent byEntity)
        {
            float modifier = byEntity.Stats.GetBlended("attackSpeed");
            if (modifier <= 0 || modifier == 1)
            {
                return secondsPassed;
            }

            return secondsPassed * modifier;
        }
    }
}
