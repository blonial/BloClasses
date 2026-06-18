using System.Runtime.CompilerServices;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace BloClasses.Patches
{
    internal static class FruitTreeCuttingAttributeNames
    {
        public const string PlaceFailure = "fruitTreeCuttingPlaceFailure";
    }

    internal sealed class FruitTreeCuttingPlacementData
    {
        public float PlaceFailure { get; set; }
    }

    internal static class FruitTreeCuttingPlacementStorage
    {
        private static readonly ConditionalWeakTable<BlockEntityFruitTreeBranch, FruitTreeCuttingPlacementData> DataByBranch = new ConditionalWeakTable<BlockEntityFruitTreeBranch, FruitTreeCuttingPlacementData>();

        public static void Set(BlockEntityFruitTreeBranch branch, float placeFailure)
        {
            FruitTreeCuttingPlacementData data = DataByBranch.GetOrCreateValue(branch);
            data.PlaceFailure = placeFailure;
        }

        public static FruitTreeCuttingPlacementData? Get(BlockEntityFruitTreeBranch branch)
        {
            return DataByBranch.TryGetValue(branch, out FruitTreeCuttingPlacementData? data) ? data : null;
        }

        public static void FromTreeAttributes(BlockEntityFruitTreeBranch branch, ITreeAttribute tree)
        {
            float placeFailure = tree.GetFloat(FruitTreeCuttingAttributeNames.PlaceFailure, 0);

            if (placeFailure == 0)
            {
                return;
            }

            FruitTreeCuttingPlacementData data = DataByBranch.GetOrCreateValue(branch);
            data.PlaceFailure = placeFailure;
        }

        public static void ToTreeAttributes(BlockEntityFruitTreeBranch branch, ITreeAttribute tree)
        {
            FruitTreeCuttingPlacementData? data = Get(branch);
            if (data == null)
            {
                return;
            }

            if (data.PlaceFailure != 0)
            {
                tree.SetFloat(FruitTreeCuttingAttributeNames.PlaceFailure, data.PlaceFailure);
            }
        }
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

            var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityFruitTreeBranch;
            if (be == null)
            {
                return;
            }

            float blendedModifier = byPlayer.Entity.Stats.GetBlended(FruitTreeCuttingAttributeNames.PlaceFailure);
            float modifier = blendedModifier == 1 ? 0 : blendedModifier - 1;
            FruitTreeCuttingPlacementStorage.Set(be, modifier);

            be.MarkDirty();
        }
    }

    [HarmonyPatch(typeof(BlockEntityFruitTreeBranch))]
    public static class FruitTreeBranchBlockEntityPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(BlockEntityFruitTreeBranch.FromTreeAttributes))]
        public static void FromTreeAttributesPostfix(BlockEntityFruitTreeBranch __instance, ITreeAttribute tree)
        {
            FruitTreeCuttingPlacementStorage.FromTreeAttributes(__instance, tree);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(BlockEntityFruitTreeBranch.ToTreeAttributes))]
        public static void ToTreeAttributesPostfix(BlockEntityFruitTreeBranch __instance, ITreeAttribute tree)
        {
            FruitTreeCuttingPlacementStorage.ToTreeAttributes(__instance, tree);
        }
    }
}
