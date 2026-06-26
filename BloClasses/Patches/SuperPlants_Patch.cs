using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace BloClasses.Patches
{
    [HarmonyPatch(typeof(BlockCrop), nameof(BlockCrop.GetDrops))]
    public static class SuperPlants_Patch
    {
        public static void Postfix(BlockCrop __instance, ref ItemStack[] __result, IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            if (byPlayer?.Entity?.WatchedAttributes == null || __result == null)
            {
                return;
            }

            if (!IsFullyGrownHealthyCrop(__instance))
            {
                return;
            }

            if (world.BlockAccessor.GetBlockEntity(pos.DownCopy()) is not BlockEntityFarmland)
            {
                return;
            }

            var charSystem = world.Api.ModLoader.GetModSystem<CharacterSystem>();
            if (charSystem?.characterClasses == null)
            {
                return;
            }

            string characterClassCode = byPlayer.Entity.WatchedAttributes.GetString("characterClass");
            var charClass = charSystem.characterClasses.Find(c => c?.Code == characterClassCode);
            if (charClass?.Traits?.Contains("bccropfanatic") != true)
            {
                return;
            }

            var extraDrops = new List<ItemStack>();

            foreach (var stack in __result)
            {
                if (stack.Collectible.Code.ToString().Contains("game:seeds"))
                {
                    if (world.Rand.NextDouble() <= 0.03)
                    {
                        extraDrops.Add(
                            new ItemStack(
                                world.GetItem(
                                    new AssetLocation(
                                        stack.Collectible.Code.ToString().Replace("game", "bloclasses")
                                    )
                                )
                            )
                        );
                    }
                }
            }

            if (extraDrops.Count > 0)
            {
                __result = __result.Concat(extraDrops).ToArray();
            }
        }

        private static bool IsFullyGrownHealthyCrop(BlockCrop crop)
        {
            int growthStages = crop.Attributes?["cropProps"]["growthStages"].AsInt(0) ?? 0;
            if (growthStages <= 0 || crop.CurrentCropStage < growthStages)
            {
                return false;
            }

            string blockPath = crop.Code?.Path ?? "";
            return !blockPath.Contains("dead") && !blockPath.Contains("wither") && !blockPath.Contains("frost");
        }
    }
}
