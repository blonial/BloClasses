using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace BloClasses.Patches
{
    [HarmonyPatch(typeof(EntityAgent))]
    public static class FallDamage_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(EntityAgent.ReceiveDamage))]
        public static void ReceiveDamagePrefix(DamageSource damageSource, ref float damage, EntityAgent __instance)
        {
            if (damage <= 0 || damageSource?.Type != EnumDamageType.Gravity)
            {
                return;
            }

            float modifier = __instance.Stats.GetBlended("fallDamageMul");
            if (modifier == 0)
            {
                return;
            }

            damage *= GameMath.Max(0, 1 + modifier);
        }
    }
}
