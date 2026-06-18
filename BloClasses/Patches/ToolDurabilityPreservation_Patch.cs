using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace BloClasses.Patches
{
    [HarmonyPatch(typeof(CollectibleObject), nameof(CollectibleObject.DamageItem), typeof(IWorldAccessor), typeof(Entity), typeof(ItemSlot), typeof(int), typeof(bool))]
    public static class ToolDurabilityPreservation_Patch
    {
        public static bool Prefix(CollectibleObject __instance, IWorldAccessor world, Entity byEntity, ItemSlot itemSlot, int amount)
        {
            if (amount <= 0 || byEntity == null || itemSlot?.Itemstack == null)
            {
                return true;
            }

            string? statCode = GetPreservationStatCode(__instance.Tool);
            if (statCode == null)
            {
                return true;
            }

            float chance = GetChance(byEntity, statCode);
            if (chance <= 0)
            {
                return true;
            }

            return world.Rand.NextDouble() >= chance;
        }

        private static string? GetPreservationStatCode(EnumTool? tool)
        {
            return tool switch
            {
                EnumTool.Hammer => "hammerDurabilityPreservationChance",
                EnumTool.Pickaxe => "pickaxeDurabilityPreservationChance",
                _ => null
            };
        }

        private static float GetChance(Entity entity, string statCode)
        {
            float blended = entity.Stats.GetBlended(statCode);
            return blended == 1f ? 0 : GameMath.Clamp(blended - 1f, 0, 1);
        }
    }
}
