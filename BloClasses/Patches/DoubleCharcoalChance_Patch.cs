using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace BloClasses.Patches
{
    [HarmonyPatch(typeof(BlockCharcoalPile), nameof(BlockCharcoalPile.OnBlockBroken))]
    public static class DoubleCharcoalChance_Patch
    {
        [HarmonyPostfix]
        public static void OnBlockBrokenPostfix(IWorldAccessor world, BlockPos pos, IPlayer byPlayer)
        {
            if (byPlayer?.Entity == null || byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative)
            {
                return;
            }

            float chance = GetExtraCharcoalChance(byPlayer);
            if (chance <= 0 || world.Rand.NextDouble() >= chance)
            {
                return;
            }

            Item? charcoal = world.GetItem(new AssetLocation("game", "charcoal"));
            if (charcoal == null)
            {
                return;
            }

            world.SpawnItemEntity(new ItemStack(charcoal), pos.ToVec3d().Add(0.5, 0.25, 0.5));
        }

        private static float GetExtraCharcoalChance(IPlayer player)
        {
            float blended = player.Entity.Stats.GetBlended("doubleCharcoalChance");
            if (blended == 1)
            {
                return 0;
            }

            return GameMath.Clamp(blended - 1, 0, 1);
        }
    }
}
