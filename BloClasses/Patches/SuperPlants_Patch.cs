using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace BloClasses.Patches
{
    [HarmonyPatch(typeof(BlockCrop), nameof(BlockCrop.GetDrops))]
    public static class SuperPlants_Patch
    {
        private const double SuperPlantSeedDropChance = 0.03;

        public static void Postfix(BlockCrop __instance, ref ItemStack[] __result, IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            if (byPlayer?.Entity?.WatchedAttributes == null || __result == null)
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

            if (!IsFullyGrownCrop(__instance) || !IsHealthyCrop(__instance))
            {
                return;
            }

            var extraDrops = new List<ItemStack>();

            foreach (var stack in __result)
            {
                if (IsVanillaSeedStack(stack))
                {
                    if (world.Rand.NextDouble() <= SuperPlantSeedDropChance)
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

        private static bool IsFullyGrownCrop(BlockCrop crop)
        {
            int growthStages = GetGrowthStages(crop);
            if (growthStages <= 0)
            {
                return false;
            }

            int? variantStage = GetVariantStage(crop);
            if (variantStage.HasValue)
            {
                return variantStage.Value >= growthStages;
            }

            int? codeStage = GetCodeStage(crop);
            if (codeStage.HasValue)
            {
                return codeStage.Value >= growthStages;
            }

            return crop.CurrentCropStage >= growthStages - 1;
        }

        private static int GetGrowthStages(BlockCrop crop)
        {
            object? cropProps = GetMemberValue(crop, "CropProps")
                ?? GetMemberValue(crop, "cropProps");

            object? growthStages = cropProps == null
                ? null
                : GetMemberValue(cropProps, "growthStages") ?? GetMemberValue(cropProps, "GrowthStages");

            return growthStages is int value ? value : 0;
        }

        private static object? GetMemberValue(object instance, string memberName)
        {
            Type type = instance.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            return type.GetProperty(memberName, flags)?.GetValue(instance)
                ?? type.GetField(memberName, flags)?.GetValue(instance);
        }

        private static int? GetVariantStage(BlockCrop crop)
        {
            return crop.Variant?.TryGetValue("stage", out string stage) == true && int.TryParse(stage, out int value)
                ? value
                : null;
        }

        private static int? GetCodeStage(BlockCrop crop)
        {
            string path = crop.Code?.Path ?? "";
            int dashIndex = path.LastIndexOf('-');
            if (dashIndex < 0 || dashIndex == path.Length - 1)
            {
                return null;
            }

            return int.TryParse(path[(dashIndex + 1)..], out int value)
                ? value
                : null;
        }

        private static bool IsHealthyCrop(BlockCrop crop)
        {
            string blockPath = crop.Code?.Path ?? "";
            return !blockPath.Contains("dead") && !blockPath.Contains("wither") && !blockPath.Contains("frost");
        }

        private static bool IsVanillaSeedStack(ItemStack stack)
        {
            AssetLocation code = stack.Collectible.Code;
            return code.Domain == "game" && code.Path.StartsWith("seeds-", StringComparison.Ordinal);
        }
    }
}
