using BloClasses.Blocks;
using HarmonyLib;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace BloClasses.Patches
{
    public static class GoodCookMealBonus_Patch
    {
        private const string SaturationMultiplierAttribute = "bloclassesCookedFoodSaturationMul";
        private const string PerishTimeMultiplierAttribute = "bloclassesCookedFoodPerishTimeMul";

        [HarmonyPatch(typeof(BlockCookingContainer), nameof(BlockCookingContainer.DoSmelt))]
        public static class CookingContainerDoSmeltPatch
        {
            public static void Prefix(BlockCookingContainer __instance, out GoodCookMealBonus? __state)
            {
                __state = null;

                if (__instance is not CustomBlockCookingContainer cookingContainer)
                {
                    return;
                }

                EntityAgent? entity = cookingContainer.LastTouchingPlayer?.Entity;
                if (entity == null)
                {
                    return;
                }

                float saturationMultiplier = entity.Stats.GetBlended("cookedFoodSaturation");
                float perishTimeStat = entity.Stats.GetBlended("cookedFoodPerishTime");
                float perishTimeMultiplier = GetTimeMultiplierFromStat(perishTimeStat);

                if (saturationMultiplier == 1f && perishTimeMultiplier == 1f)
                {
                    return;
                }

                __state = new GoodCookMealBonus(saturationMultiplier, perishTimeMultiplier);
            }

            public static void Postfix(IWorldAccessor world, ItemSlot outputSlot, GoodCookMealBonus? __state)
            {
                if (__state == null || outputSlot.Itemstack == null)
                {
                    return;
                }

                ApplyToCookedContainer(world, outputSlot.Itemstack, __state);
            }
        }

        [HarmonyPatch(typeof(BlockMeal), nameof(BlockMeal.GetContentNutritionProperties), typeof(IWorldAccessor), typeof(ItemSlot), typeof(ItemStack[]), typeof(EntityAgent), typeof(bool), typeof(float), typeof(float))]
        public static class MealNutritionPatch
        {
            public static void Postfix(ItemSlot inSlot, ItemStack[] contentStacks, ref FoodNutritionProperties[] __result)
            {
                if (__result == null || __result.Length == 0)
                {
                    return;
                }

                float saturationMultiplier = GetSaturationMultiplier(inSlot?.Itemstack, contentStacks);
                if (saturationMultiplier == 1f)
                {
                    return;
                }

                foreach (FoodNutritionProperties nutrition in __result)
                {
                    nutrition.Satiety *= saturationMultiplier;
                }
            }
        }

        private static void ApplyToCookedContainer(IWorldAccessor world, ItemStack cookedContainerStack, GoodCookMealBonus bonus)
        {
            if (cookedContainerStack.Collectible is not BlockCookedContainerBase cookedContainer)
            {
                return;
            }

            ItemStack[] contents = cookedContainer.GetNonEmptyContents(world, cookedContainerStack);
            if (contents.Length == 0)
            {
                return;
            }

            SetBonusAttributes(cookedContainerStack, bonus);

            foreach (ItemStack contentStack in contents)
            {
                SetBonusAttributes(contentStack, bonus);
                ApplyPerishTimeMultiplier(world, contentStack, bonus.PerishTimeMultiplier);
            }

            string? recipeCode = cookedContainerStack.Attributes.GetString("recipeCode", null);
            float quantityServings = cookedContainerStack.Attributes.GetFloat("quantityServings", 1f);
            cookedContainer.SetContents(recipeCode, quantityServings, cookedContainerStack, contents);
        }

        private static void SetBonusAttributes(ItemStack stack, GoodCookMealBonus bonus)
        {
            if (bonus.SaturationMultiplier != 1f)
            {
                stack.Attributes.SetFloat(SaturationMultiplierAttribute, bonus.SaturationMultiplier);
            }

            if (bonus.PerishTimeMultiplier != 1f)
            {
                stack.Attributes.SetFloat(PerishTimeMultiplierAttribute, bonus.PerishTimeMultiplier);
            }
        }

        private static void ApplyPerishTimeMultiplier(IWorldAccessor world, ItemStack stack, float multiplier)
        {
            if (multiplier == 1f)
            {
                return;
            }

            stack.Collectible.UpdateAndGetTransitionState(world, new DummySlot(stack), EnumTransitionType.Perish);

            if (stack.Attributes["transitionstate"] is not ITreeAttribute transitionState)
            {
                return;
            }

            ScaleFloatArray(transitionState, "freshHours", multiplier);
            ScaleFloatArray(transitionState, "transitionHours", multiplier);
            ScaleFloatArray(transitionState, "transitionedHours", multiplier);
        }

        private static void ScaleFloatArray(ITreeAttribute tree, string key, float multiplier)
        {
            if (tree[key] is not FloatArrayAttribute attribute)
            {
                return;
            }

            for (int i = 0; i < attribute.value.Length; i++)
            {
                attribute.value[i] *= multiplier;
            }
        }

        private static float GetSaturationMultiplier(ItemStack? containerStack, ItemStack[]? contentStacks)
        {
            float multiplier = containerStack?.Attributes.GetFloat(SaturationMultiplierAttribute, 1f) ?? 1f;

            if (contentStacks != null)
            {
                foreach (ItemStack contentStack in contentStacks)
                {
                    multiplier = Math.Max(multiplier, contentStack?.Attributes.GetFloat(SaturationMultiplierAttribute, 1f) ?? 1f);
                }
            }

            return multiplier;
        }

        private static float GetTimeMultiplierFromStat(float statValue)
        {
            if (statValue == 0f || statValue == 1f)
            {
                return 1f;
            }

            return statValue < 1f ? 1f + (1f - statValue) : statValue;
        }

        public sealed class GoodCookMealBonus
        {
            public GoodCookMealBonus(float saturationMultiplier, float perishTimeMultiplier)
            {
                SaturationMultiplier = Math.Max(0f, saturationMultiplier);
                PerishTimeMultiplier = Math.Max(0f, perishTimeMultiplier);
            }

            public float SaturationMultiplier { get; }

            public float PerishTimeMultiplier { get; }
        }
    }
}
