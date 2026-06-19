using BloClasses.Extensions;
using HarmonyLib;
using System.Runtime.CompilerServices;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace BloClasses.Patches
{
    [HarmonyPatch(typeof(BlockBarrel), nameof(BlockBarrel.OnBlockInteractStart))]
    public static class BarrelSommelierAlcohol_BlockInteract_Patch
    {
        public static void Prefix(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityBarrel barrel && !barrel.Sealed)
            {
                BarrelSommelierAlcoholState.SetRichAlcoholProduction(barrel, TraitRequirementUtil.PlayerHasTrait(byPlayer, "bcsommelier"));
            }
        }
    }

    [HarmonyPatch(typeof(BlockEntityBarrel), nameof(BlockEntityBarrel.OnReceivedClientPacket))]
    public static class BarrelSommelierAlcohol_ClientPacket_Patch
    {
        public static void Prefix(BlockEntityBarrel __instance, IPlayer player)
        {
            if (!__instance.Sealed)
            {
                BarrelSommelierAlcoholState.SetRichAlcoholProduction(__instance, TraitRequirementUtil.PlayerHasTrait(player, "bcsommelier"));
            }
        }
    }

    [HarmonyPatch(typeof(BlockEntityBarrel), "FindMatchingRecipe", new System.Type[] { })]
    public static class BarrelSommelierAlcohol_NoPlayer_Patch
    {
        public static void Postfix(BlockEntityBarrel __instance)
        {
            BarrelSommelierAlcoholState.ApplyRichAlcoholRecipe(__instance);
        }
    }

    [HarmonyPatch(typeof(BlockEntityBarrel), nameof(BlockEntityBarrel.GetCanSeal), typeof(IPlayer))]
    public static class BarrelSommelierAlcohol_GetCanSeal_Patch
    {
        public static void Postfix(BlockEntityBarrel __instance, IPlayer byPlayer)
        {
            if (!__instance.Sealed)
            {
                BarrelSommelierAlcoholState.SetRichAlcoholProduction(__instance, TraitRequirementUtil.PlayerHasTrait(byPlayer, "bcsommelier"));
            }
        }
    }

    [HarmonyPatch(typeof(BlockEntityBarrel), nameof(BlockEntityBarrel.SealBarrel))]
    public static class BarrelSommelierAlcohol_SealBarrel_Patch
    {
        public static void Postfix(BlockEntityBarrel __instance)
        {
            BarrelSommelierAlcoholState.ApplyRichAlcoholRecipe(__instance);
        }
    }

    [HarmonyPatch(typeof(BlockEntityBarrel), nameof(BlockEntityBarrel.ToTreeAttributes))]
    public static class BarrelSommelierAlcohol_ToTreeAttributes_Patch
    {
        public static void Postfix(BlockEntity __instance, ITreeAttribute tree)
        {
            if (__instance is BlockEntityBarrel barrel)
            {
                tree.SetBool("bloClassesRichAlcoholProduction", BarrelSommelierAlcoholState.GetRichAlcoholProduction(barrel));
            }
        }
    }

    [HarmonyPatch(typeof(BlockEntityBarrel), nameof(BlockEntityBarrel.FromTreeAttributes))]
    public static class BarrelSommelierAlcohol_FromTreeAttributes_Patch
    {
        public static void Postfix(BlockEntity __instance, ITreeAttribute tree)
        {
            if (__instance is BlockEntityBarrel barrel)
            {
                BarrelSommelierAlcoholState.SetRichAlcoholProduction(barrel, tree.GetBool("bloClassesRichAlcoholProduction"));
                BarrelSommelierAlcoholState.ApplyRichAlcoholRecipe(barrel);
            }
        }
    }

    internal static class BarrelSommelierAlcoholState
    {
        private static readonly ConditionalWeakTable<BlockEntityBarrel, State> States = new();

        public static bool GetRichAlcoholProduction(BlockEntityBarrel barrel)
        {
            return States.TryGetValue(barrel, out var state) && state.RichAlcoholProduction;
        }

        public static void SetRichAlcoholProduction(BlockEntityBarrel barrel, bool enabled)
        {
            States.GetOrCreateValue(barrel).RichAlcoholProduction = enabled;
        }

        public static void ApplyRichAlcoholRecipe(BlockEntityBarrel barrel)
        {
            if (!GetRichAlcoholProduction(barrel))
            {
                return;
            }

            var recipe = barrel.CurrentRecipe;
            var output = recipe?.Output;
            var outputCode = output?.Code;
            if (recipe == null || output == null || outputCode == null || outputCode.Path == null)
            {
                return;
            }

            if (!outputCode.Path.StartsWith("ciderportion-") || outputCode.Path.StartsWith("richciderportion-"))
            {
                return;
            }

            var richOutput = output.Clone();
            richOutput.Code = new AssetLocation("bloclasses", "rich" + outputCode.Path);
            richOutput.Resolve(barrel.Api.World, "sommelier barrel recipe output");

            barrel.CurrentRecipe = new BarrelRecipe
            {
                Code = recipe.Code + "-sommelier",
                SealHours = recipe.SealHours,
                Ingredients = recipe.Ingredients,
                Output = richOutput
            };
        }

        private sealed class State
        {
            public bool RichAlcoholProduction { get; set; }
        }
    }
}
