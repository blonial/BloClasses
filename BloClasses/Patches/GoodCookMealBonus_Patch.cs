using BloClasses.Blocks;
using HarmonyLib;
using System;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace BloClasses.Patches
{
    public static class GoodCookMealBonus_Patch
    {
        [HarmonyPatch(typeof(BlockCookingContainer), nameof(BlockCookingContainer.DoSmelt))]
        public static class CookingContainerDoSmeltPatch
        {
            public static void Prefix(BlockCookingContainer __instance, ItemSlot inputSlot, out GoodCookMealBonus? __state)
            {
                __state = null;

                if (__instance is not CustomBlockCookingContainer cookingContainer)
                {
                    return;
                }

                __state = GetBonusFromStack(inputSlot.Itemstack);
                if (__state != null)
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

        [HarmonyPatch(typeof(BlockCookedContainerBase), "GetContentInDummySlot")]
        public static class CookedContainerTransitionSpeedPatch
        {
            public static void Postfix(ItemSlot inslot, ItemStack itemstack, ItemSlot __result)
            {
                AddGoodCookPerishSpeedModifier(inslot, itemstack, __result);
            }
        }

        [HarmonyPatch(typeof(BlockMeal), "GetContentInDummySlot")]
        public static class MealTransitionSpeedPatch
        {
            public static void Postfix(ItemSlot inslot, ItemStack itemstack, ItemSlot __result)
            {
                AddGoodCookPerishSpeedModifier(inslot, itemstack, __result);
            }
        }

        [HarmonyPatch(typeof(BlockCookedContainerBase), nameof(BlockCookedContainerBase.ServeIntoStack))]
        public static class ServeIntoStackPatch
        {
            public static void Prefix(ItemSlot potslot, out GoodCookMealBonus? __state)
            {
                __state = GetBonusFromStack(potslot?.Itemstack);
            }

            public static void Postfix(bool __result, ItemSlot bowlSlot, GoodCookMealBonus? __state)
            {
                if (!__result || __state == null || bowlSlot?.Itemstack == null)
                {
                    return;
                }

                SetBonusAttributes(bowlSlot.Itemstack, __state);
            }
        }

        private static void ApplyToCookedContainer(IWorldAccessor world, ItemStack cookedContainerStack, GoodCookMealBonus bonus)
        {
            SetBonusAttributes(cookedContainerStack, bonus);
        }

        private static void SetBonusAttributes(ItemStack stack, GoodCookMealBonus bonus)
        {
            if (bonus.SaturationMultiplier != 1f)
            {
                stack.Attributes.SetFloat(CustomBlockCookingContainer.CookedFoodSaturationMultiplierAttribute, bonus.SaturationMultiplier);
            }

            if (bonus.PerishTimeMultiplier != 1f)
            {
                stack.Attributes.SetFloat(CustomBlockCookingContainer.CookedFoodPerishTimeMultiplierAttribute, bonus.PerishTimeMultiplier);
            }
        }

        private static void AddGoodCookPerishSpeedModifier(ItemSlot containerSlot, ItemStack contentStack, ItemSlot dummySlot)
        {
            if (dummySlot?.Inventory == null)
            {
                return;
            }

            dummySlot.Inventory.OnAcquireTransitionSpeed += (transitionType, stack, mul) =>
            {
                if (transitionType != EnumTransitionType.Perish)
                {
                    return mul;
                }

                float multiplier = GetPerishTimeMultiplier(containerSlot?.Itemstack, stack ?? contentStack);
                return multiplier <= 0f ? mul : mul / multiplier;
            };
        }

        private static float GetSaturationMultiplier(ItemStack? containerStack, ItemStack[]? contentStacks)
        {
            float multiplier = containerStack?.Attributes.GetFloat(CustomBlockCookingContainer.CookedFoodSaturationMultiplierAttribute, 1f) ?? 1f;

            if (contentStacks != null)
            {
                foreach (ItemStack contentStack in contentStacks)
                {
                    multiplier = Math.Max(multiplier, contentStack?.Attributes.GetFloat(CustomBlockCookingContainer.CookedFoodSaturationMultiplierAttribute, 1f) ?? 1f);
                }
            }

            return multiplier;
        }

        private static float GetPerishTimeMultiplier(ItemStack? containerStack, ItemStack? contentStack)
        {
            float multiplier = containerStack?.Attributes.GetFloat(CustomBlockCookingContainer.CookedFoodPerishTimeMultiplierAttribute, 1f) ?? 1f;
            multiplier = Math.Max(multiplier, contentStack?.Attributes.GetFloat(CustomBlockCookingContainer.CookedFoodPerishTimeMultiplierAttribute, 1f) ?? 1f);

            return multiplier;
        }

        private static GoodCookMealBonus? GetBonusFromStack(ItemStack? itemStack)
        {
            if (itemStack == null)
            {
                return null;
            }

            float saturationMultiplier = itemStack.Attributes.GetFloat(CustomBlockCookingContainer.CookedFoodSaturationMultiplierAttribute, 1f);
            float perishTimeMultiplier = itemStack.Attributes.GetFloat(CustomBlockCookingContainer.CookedFoodPerishTimeMultiplierAttribute, 1f);

            return saturationMultiplier == 1f && perishTimeMultiplier == 1f
                ? null
                : new GoodCookMealBonus(saturationMultiplier, perishTimeMultiplier);
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
