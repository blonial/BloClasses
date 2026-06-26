using BloClasses.BlockEntities;
using BloClasses.Blocks;
using BloClasses.EntityBehaviors;
using BloClasses.Patches;
using HarmonyLib;
using System;
using Vintagestory.API.Common;

namespace BloClasses
{
    public class BloClassesModSystem : ModSystem
    {
        private static Harmony? CommonPatcher;
        private static Harmony? ClientPatcher;

        private static readonly Type[] CommonPatchTypes =
        {
            typeof(AttackSpeed_Patch),
            typeof(FruitTreeBranchPatches),
            typeof(BarrelSommelierAlcohol_BlockInteract_Patch),
            typeof(BarrelSommelierAlcohol_ClientPacket_Patch),
            typeof(BarrelSommelierAlcohol_NoPlayer_Patch),
            typeof(BarrelSommelierAlcohol_GetCanSeal_Patch),
            typeof(BarrelSommelierAlcohol_SealBarrel_Patch),
            typeof(ClayFormingRequiresTrait_Patch.OnReceivedClientPacketPatch),
            typeof(ClayFormingRequiresTrait_Patch.PutClayPatch),
            typeof(ClayFormingRequiresTrait_Patch.CheckIfFinishedPatch),
            typeof(ClayFormingRequiresTrait_Patch.OnBeginUsePatch),
            typeof(ClayFormingRequiresTrait_Patch.OnUseOverLayerPatch),
            typeof(ClayFormingRequiresTrait_Patch.OnUseOverVoxelPatch),
            typeof(FreezeResistance_Patch),
            typeof(FireDamage_Patch),
            typeof(FallDamage_Patch),
            typeof(FarmingSeedDropRate_Patch),
            typeof(FarmingCropDropRate_Patch),
            typeof(PlowTime_Patch),
            typeof(PanningSpeed_Patch),
            typeof(DoubleCharcoalChance_Patch),
            typeof(GoodCookMealBonus_Patch.CookingContainerDoSmeltPatch),
            typeof(GoodCookMealBonus_Patch.MealNutritionPatch),
            typeof(GoodCookMealBonus_Patch.CookedContainerTransitionSpeedPatch),
            typeof(GoodCookMealBonus_Patch.MealTransitionSpeedPatch),
            typeof(GoodCookMealBonus_Patch.ServeIntoStackPatch),
            typeof(PanningDrop_Patch),
            typeof(InventorySmelting_GetOutputText_Patch),
            typeof(StoneDrop_Patch),
            typeof(SoilDestroyingTime_Patch),
            typeof(SwimmingSpeed_Patch),
            typeof(SuperPlants_Patch),
            typeof(SneakSpeed_Patch),
            typeof(FruitTreeGrowingBranchBHPatches),
            typeof(ToolDurabilityPreservation_Patch),
            typeof(ToolDurability_Patch.ForgedToolHeadPatch),
            typeof(ToolDurability_Patch.GiveForgedToolHeadPatch),
            typeof(ToolDurability_Patch.CraftedToolPatch),
            typeof(ToolDurability_Patch.CraftingOutputPreviewPatch),
            typeof(ToolDurability_Patch.MaxToolDurabilityPatch),
            typeof(ToolDurability_Patch.ToolHeadInfoPatch),
            typeof(TreeCuttingSpeed_Patch),
            typeof(WalkingStickWalkSpeed_Patch),
            typeof(QuenchBreakChance_Patch.QuenchCoolingContextPatch),
            typeof(QuenchBreakChance_Patch.QuenchInfoContextPatch),
            typeof(QuenchBreakChance_Patch.QuenchShatterChancePatch),
            typeof(QuenchBreakChance_Patch.QuenchStoredShatterChancePatch),
            typeof(QuenchBreakChance_Patch.QuenchTemperedStatsPatch),
        };

        public override void Start(ICoreAPI api)
        {
            api.RegisterBlockClass("CustomBlockCookingContainer", typeof(CustomBlockCookingContainer));

            api.RegisterBlockEntityClass("CustomBlockEntityBarrel", typeof(CustomBlockEntityBarrel));
            api.RegisterBlockEntityClass("CustomBlockEntityFirepit", typeof(CustomBlockEntityFirepit));
            api.RegisterBlockEntityClass("CustomBlockEntityFruitTreeBranch", typeof(CustomBlockEntityFruitTreeBranch));

            api.RegisterEntityBehaviorClass("UndergroundStabilityLossModEntityBehavior", typeof(UndergroundStabilityLossModEntityBehavior));
            api.RegisterEntityBehaviorClass("WalkingStickHeldBonusEntityBehavior", typeof(WalkingStickHeldBonusEntityBehavior));

            if (CommonPatcher == null)
            {
                CommonPatcher = new Harmony(Mod.Info.ModID + ".common");
                PatchTypes(CommonPatcher, CommonPatchTypes);
            }

            if (api.Side == EnumAppSide.Client && ClientPatcher == null)
            {
                ClientPatcher = new Harmony(Mod.Info.ModID + ".client");
                PatchTypes(ClientPatcher, GetClientPatchTypes());
            }
        }

        public override void Dispose()
        {
            CommonPatcher?.UnpatchAll(Mod.Info.ModID + ".common");
            CommonPatcher = null;

            ClientPatcher?.UnpatchAll(Mod.Info.ModID + ".client");
            ClientPatcher = null;
        }

        private static void PatchTypes(Harmony patcher, Type[] patchTypes)
        {
            foreach (Type patchType in patchTypes)
            {
                patcher.CreateClassProcessor(patchType).Patch();
            }
        }

        private static Type[] GetClientPatchTypes()
        {
            return new[]
            {
                typeof(BarrelSommelierAlcohol_GuiContentsText_Patch),
                typeof(ClayFormingRequiresTrait_Patch.GetClayformingRecipesPatch),
                typeof(ClayFormingRequiresTrait_Patch.CanDoRecipePatch),
                typeof(GuiHandbookMealRecipePage_addGeneralInfo_Patch),
                typeof(CollectibleBehaviorHandbookTextAndExtraInfo_AddGeneralInfo_Patch),
            };
        }
    }
}
