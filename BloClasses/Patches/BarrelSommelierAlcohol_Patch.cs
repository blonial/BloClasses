using BloClasses.BlockEntities;
using BloClasses.Extensions;
using HarmonyLib;
using System.Runtime.CompilerServices;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
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

    [HarmonyPatch(typeof(GuiDialogBarrel), "getContentsText")]
    public static class BarrelSommelierAlcohol_GuiContentsText_Patch
    {
        private static readonly AccessTools.FieldRef<GuiDialog, ICoreClientAPI> CapiField =
            AccessTools.FieldRefAccess<GuiDialog, ICoreClientAPI>("capi");

        public static void Prefix(GuiDialogBarrel __instance, out BarrelRecipe? __state)
        {
            __state = null;

            ICoreClientAPI? capi = CapiField(__instance);
            IPlayer? player = capi?.World?.Player;
            if (player == null || !TraitRequirementUtil.PlayerHasTrait(player, "bcsommelier"))
            {
                return;
            }

            BlockPos pos = __instance.BlockEntityPosition;
            if (capi?.World?.BlockAccessor.GetBlockEntity(pos) is not BlockEntityBarrel barrel || barrel.Sealed)
            {
                return;
            }

            BarrelRecipe? richRecipe = BarrelSommelierAlcoholState.CreateRichAlcoholRecipe(barrel, barrel.CurrentRecipe);
            if (richRecipe == null)
            {
                return;
            }

            __state = barrel.CurrentRecipe;
            barrel.CurrentRecipe = richRecipe;
        }

        public static void Postfix(GuiDialogBarrel __instance, BarrelRecipe? __state)
        {
            if (__state == null)
            {
                return;
            }

            ICoreClientAPI? capi = CapiField(__instance);
            BlockPos pos = __instance.BlockEntityPosition;
            if (capi?.World?.BlockAccessor.GetBlockEntity(pos) is BlockEntityBarrel barrel)
            {
                barrel.CurrentRecipe = __state;
            }
        }
    }

    internal static class BarrelSommelierAlcoholState
    {
        private static readonly ConditionalWeakTable<BlockEntityBarrel, State> States = new();

        public static bool GetRichAlcoholProduction(BlockEntityBarrel barrel)
        {
            if (barrel is CustomBlockEntityBarrel customBarrel)
            {
                return customBarrel.RichAlcoholProduction;
            }

            return States.TryGetValue(barrel, out var state) && state.RichAlcoholProduction;
        }

        public static void SetRichAlcoholProduction(BlockEntityBarrel barrel, bool enabled)
        {
            if (barrel is CustomBlockEntityBarrel customBarrel)
            {
                customBarrel.RichAlcoholProduction = enabled;
                customBarrel.MarkDirty();
                return;
            }

            States.GetOrCreateValue(barrel).RichAlcoholProduction = enabled;
            barrel.MarkDirty();
        }

        public static void ApplyRichAlcoholRecipe(BlockEntityBarrel barrel)
        {
            if (!GetRichAlcoholProduction(barrel))
            {
                return;
            }

            BarrelRecipe? richRecipe = CreateRichAlcoholRecipe(barrel, barrel.CurrentRecipe);
            if (richRecipe != null)
            {
                barrel.CurrentRecipe = richRecipe;
            }
        }

        public static BarrelRecipe? CreateRichAlcoholRecipe(BlockEntityBarrel barrel, BarrelRecipe? recipe)
        {
            var output = recipe?.Output;
            var outputCode = output?.Code;
            if (recipe == null || output == null || outputCode == null || outputCode.Path == null)
            {
                return null;
            }

            if (!outputCode.Path.StartsWith("ciderportion-") || outputCode.Path.StartsWith("richciderportion-"))
            {
                return null;
            }

            var richOutput = output.Clone();
            richOutput.Code = new AssetLocation("bloclasses", "rich" + outputCode.Path);
            richOutput.Resolve(barrel.Api.World, "sommelier barrel recipe output");

            return new BarrelRecipe
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
