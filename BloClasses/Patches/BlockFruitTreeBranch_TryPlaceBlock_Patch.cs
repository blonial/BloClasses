using BloClasses.BlockEntities;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace BloClasses.Patches
{
    internal static class FruitTreeCuttingAttributeNames
    {
        public const string PlaceFailure = "fruitTreeCuttingPlaceFailure";
    }

    [HarmonyPatch(typeof(BlockFruitTreeBranch))]
    public static class FruitTreeBranchPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(BlockFruitTreeBranch.TryPlaceBlock))]
        public static void Postfix(bool __result, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (!__result || byPlayer?.Entity == null)
            {
                return;
            }

            CustomBlockEntityFruitTreeBranch? be = GetPlacedBranchEntity(world, blockSel);
            if (be == null)
            {
                return;
            }

            float blendedModifier = byPlayer.Entity.Stats.GetBlended(FruitTreeCuttingAttributeNames.PlaceFailure);
            be.FruitTreeCuttingPlaceFailure = blendedModifier == 1 ? 0 : blendedModifier - 1;
            be.MarkDirty();
        }

        private static CustomBlockEntityFruitTreeBranch? GetPlacedBranchEntity(IWorldAccessor world, BlockSelection blockSel)
        {
            var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as CustomBlockEntityFruitTreeBranch;
            if (be != null || blockSel.Face == null)
            {
                return be;
            }

            return world.BlockAccessor.GetBlockEntity(blockSel.Position.AddCopy(blockSel.Face)) as CustomBlockEntityFruitTreeBranch;
        }
    }
}
