using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BloClasses.Extensions;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace BloClasses.Patches
{
    public static class ClayFormingRequiresTrait_Patch
    {
        [HarmonyPatch(typeof(ApiAdditions), nameof(ApiAdditions.GetClayformingRecipes))]
        public static class GetClayformingRecipesPatch
        {
            public static void Postfix(ICoreAPI api, ref List<ClayFormingRecipe> __result)
            {
                if (api is not ICoreClientAPI capi || capi.World?.Player == null)
                {
                    return;
                }

                __result = __result.Where(recipe => CanUseRecipe(api, capi.World.Player, recipe)).ToList();
            }
        }

        [HarmonyPatch(typeof(BlockEntityClayForm), "CanDoRecipe")]
        public static class CanDoRecipePatch
        {
            public static void Postfix(IClientWorldAccessor world, IRecipeBase recipe, ref bool __result)
            {
                if (!__result || recipe is not ClayFormingRecipe clayRecipe)
                {
                    return;
                }

                __result = CanUseRecipe(world.Api, world.Player, clayRecipe);
            }
        }

        [HarmonyPatch(typeof(BlockEntityClayForm), nameof(BlockEntityClayForm.OnReceivedClientPacket))]
        public static class OnReceivedClientPacketPatch
        {
            public static bool Prefix(BlockEntityClayForm __instance, IPlayer player, int packetid, byte[] data)
            {
                if (packetid != (int)EnumClayFormingPacket.SelectRecipe || data.Length < 4)
                {
                    return true;
                }

                int recipeId = SerializerUtil.Deserialize<int>(data);
                ClayFormingRecipe? recipe = ApiAdditions.GetClayformingRecipes(player.Entity.World.Api).Find(r => r.RecipeId == recipeId);
                return recipe == null || CanUseRecipe(player.Entity.World.Api, player, recipe);
            }
        }

        [HarmonyPatch(typeof(BlockEntityClayForm), nameof(BlockEntityClayForm.PutClay))]
        public static class PutClayPatch
        {
            public static bool Prefix(BlockEntityClayForm __instance, ItemSlot slot)
            {
                if (slot?.Inventory is not InventoryBasePlayer inventory)
                {
                    return true;
                }

                return CanUseSelectedRecipe(__instance, inventory.Player);
            }
        }

        [HarmonyPatch(typeof(BlockEntityClayForm), nameof(BlockEntityClayForm.CheckIfFinished))]
        public static class CheckIfFinishedPatch
        {
            public static bool Prefix(BlockEntityClayForm __instance, IPlayer byPlayer)
            {
                return CanUseSelectedRecipe(__instance, byPlayer);
            }
        }

        [HarmonyPatch(typeof(BlockEntityClayForm), nameof(BlockEntityClayForm.OnBeginUse))]
        public static class OnBeginUsePatch
        {
            public static bool Prefix(BlockEntityClayForm __instance, IPlayer byPlayer)
            {
                return CanUseSelectedRecipe(__instance, byPlayer);
            }
        }

        [HarmonyPatch(typeof(BlockEntityClayForm), nameof(BlockEntityClayForm.OnUseOver), typeof(IPlayer), typeof(int), typeof(BlockFacing), typeof(bool))]
        public static class OnUseOverLayerPatch
        {
            public static bool Prefix(BlockEntityClayForm __instance, IPlayer byPlayer)
            {
                return CanUseSelectedRecipe(__instance, byPlayer);
            }
        }

        [HarmonyPatch(typeof(BlockEntityClayForm), nameof(BlockEntityClayForm.OnUseOver), typeof(IPlayer), typeof(Vec3i), typeof(BlockFacing), typeof(bool))]
        public static class OnUseOverVoxelPatch
        {
            public static bool Prefix(BlockEntityClayForm __instance, IPlayer byPlayer)
            {
                return CanUseSelectedRecipe(__instance, byPlayer);
            }
        }

        private static bool CanUseSelectedRecipe(BlockEntityClayForm clayForm, IPlayer byPlayer)
        {
            string? requiredTrait = GetRequiredTrait(byPlayer.Entity.World.Api, clayForm.SelectedRecipe);
            return TraitRequirementUtil.PlayerHasTrait(byPlayer, requiredTrait);
        }

        private static bool CanUseRecipe(ICoreAPI api, IPlayer player, ClayFormingRecipe recipe)
        {
            string? requiredTrait = GetRequiredTrait(api, recipe);
            return TraitRequirementUtil.PlayerHasTrait(player, requiredTrait);
        }

        private static string? GetRequiredTrait(ICoreAPI api, ClayFormingRecipe? recipe)
        {
            if (!string.IsNullOrEmpty(recipe?.RequiresTrait))
            {
                return recipe.RequiresTrait;
            }

            string? outputCode = GetOutputCode(recipe);
            if (outputCode == null)
            {
                return null;
            }

            return api.GetClayFormingRecipesTraitRequirements()
                .FirstOrDefault(requirement => MatchesOutputCode(requirement.OutputCode, outputCode))
                ?.RequiresTrait;
        }

        private static string? GetOutputCode(ClayFormingRecipe? recipe)
        {
            return recipe?.Output?.Code?.ToString();
        }

        private static bool MatchesOutputCode(string? recipeOutputCode, string outputCode)
        {
            if (recipeOutputCode == null)
            {
                return false;
            }

            if (MatchesNormalizedOutputCode(recipeOutputCode, outputCode))
            {
                return true;
            }

            return !HasDomain(recipeOutputCode) || !HasDomain(outputCode)
                ? MatchesNormalizedOutputCode(WithoutDomain(recipeOutputCode), WithoutDomain(outputCode))
                : false;
        }

        private static bool MatchesNormalizedOutputCode(string patternCode, string outputCode)
        {
            string wildcard = patternCode.Replace("{color}", "*");
            string pattern = "^" + Regex.Escape(wildcard).Replace("\\*", ".*") + "$";
            return Regex.IsMatch(outputCode, pattern);
        }

        private static string WithoutDomain(string code)
        {
            int separatorIndex = code.IndexOf(':');
            return separatorIndex < 0 ? code : code[(separatorIndex + 1)..];
        }

        private static bool HasDomain(string code)
        {
            return code.IndexOf(':') >= 0;
        }
    }
}
