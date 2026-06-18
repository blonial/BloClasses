using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace BloClasses.Patches
{
    [HarmonyPatch(typeof(BlockRockTyped), nameof(BlockRockTyped.GetDrops))]
    public static class StoneDrop_Patch
    {
        [HarmonyPostfix]
        public static void GetDropsPostfix(ref ItemStack[] __result, IWorldAccessor world, IPlayer byPlayer)
        {
            if (__result == null || byPlayer?.Entity == null)
            {
                return;
            }

            float modifier = byPlayer.Entity.Stats.GetBlended("stoneDrop");
            if (modifier <= 0 || modifier == 1)
            {
                return;
            }

            foreach (ItemStack stack in __result)
            {
                if (stack?.Collectible?.Code?.Path.StartsWith("stone-") != true)
                {
                    continue;
                }

                stack.StackSize = GameMath.RoundRandom(world.Rand, stack.StackSize * modifier);
            }
        }
    }
}
