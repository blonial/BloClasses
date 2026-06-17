using HarmonyLib;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace BloClasses.Patches
{
    [HarmonyPatch(typeof(BlockCrop), "GetDrops")]
    public static class FarmingCropDropRate_Patch
    {
        public static void Postfix(ref ItemStack[] __result, IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            if (byPlayer?.Entity == null || __result == null || pos == null) return;

            var cropBonusAttribute = byPlayer.Entity.Stats.Where(s => s.Key == "farmingCropDropRate");
            if (!cropBonusAttribute.Any())
            {
                return;
            }

            if (!(world.BlockAccessor.GetBlockEntity(pos.DownCopy()) is BlockEntityFarmland))
            {
                return;
            }

            var cropBonus = cropBonusAttribute.First().Value.GetBlended();

            foreach (var stack in __result)
            {
                if (stack.Collectible.Code.Path.Contains("seed"))
                {
                    continue;
                }

                stack.StackSize = GameMath.RoundRandom(
                    world.Rand,
                    stack.StackSize * cropBonus
                );
            }
        }
    }
}
