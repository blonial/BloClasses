using System.Linq;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace BloClasses.Patches
{
    [HarmonyPatch(typeof(BlockCrop), nameof(BlockCrop.GetDrops))]
    public static class FarmingSeedDropRate_Patch
    {
        public static void Postfix(ref ItemStack[] __result, IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            if (byPlayer?.Entity == null || __result == null || pos == null)
            {
                return;
            }

            var seedBonusAttribute = byPlayer.Entity.Stats.Where(s => s.Key == "farmingSeedDropRate");
            if (!seedBonusAttribute.Any())
            {
                return;
            }

            if (world.BlockAccessor.GetBlockEntity(pos.DownCopy()) is not BlockEntityFarmland)
            {
                return;
            }

            var seedBonus = seedBonusAttribute.First().Value.GetBlended();

            foreach (var stack in __result)
            {
                if (!stack.Collectible.Code.Path.Contains("seed"))
                {
                    continue;
                }

                stack.StackSize = GameMath.RoundRandom(
                    world.Rand,
                    stack.StackSize * seedBonus
                );
            }
        }
    }
}
