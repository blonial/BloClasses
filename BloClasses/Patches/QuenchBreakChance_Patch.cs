using System;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace BloClasses.Patches
{
    public static class QuenchBreakChance_Patch
    {
        private const float MetallurgistQuenchBreakChancePerIteration = 0.04f;
        private const float DefaultQuenchBreakChancePerIteration = 0.05f;
        private const string MetallurgistTrait = "bcsuccessfulblacksmith";
        private const string ShatterChanceAttribute = "shatterchance";
        private const string QuenchBreakChancePerIterationAttribute = "bcsQuenchBreakChancePerIteration";

        [ThreadStatic]
        private static float CurrentQuenchBreakChancePerIteration;

        [ThreadStatic]
        private static float CurrentShatterChanceBeforeQuench;

        [ThreadStatic]
        private static bool HasCurrentShatterChanceBeforeQuench;

        [HarmonyPatch(typeof(CollectibleBehaviorQuenchable), "CoolToTemperature")]
        public static class QuenchCoolingContextPatch
        {
            public static void Prefix(object[] __args)
            {
                ClearQuenchApplyContext();
                CurrentQuenchBreakChancePerIteration = GetQuenchBreakChancePerIteration(
                    __args.Length > 1 ? __args[1] as ItemSlot : null
                );
            }

            public static void Finalizer()
            {
                ClearContext();
            }
        }

        [HarmonyPatch(typeof(CollectibleBehaviorQuenchable), nameof(CollectibleBehaviorQuenchable.GetHeldItemInfo))]
        public static class QuenchInfoContextPatch
        {
            public static void Prefix(object[] __args)
            {
                IWorldAccessor? world = __args.Length > 2 ? __args[2] as IWorldAccessor : null;
                CurrentQuenchBreakChancePerIteration = GetQuenchBreakChancePerIteration(
                    __args.Length > 0 ? __args[0] as ItemSlot : null,
                    world
                );
            }

            public static void Postfix(object[] __args)
            {
                if (CurrentQuenchBreakChancePerIteration <= 0 || __args.Length < 2 || __args[0] is not ItemSlot slot || __args[1] is not StringBuilder dsc)
                {
                    return;
                }

                ItemStack? itemstack = slot.Itemstack;
                if (itemstack == null)
                {
                    return;
                }

                float storedChance = itemstack.Attributes.GetFloat(ShatterChanceAttribute, DefaultQuenchBreakChancePerIteration);
                string storedLine = Lang.Get("quenchable-shatter-chance", storedChance);
                string contextualLine = Lang.Get("quenchable-shatter-chance", GetContextualShatterChance(itemstack, storedChance));

                dsc.Replace(storedLine, contextualLine);
            }

            public static void Finalizer()
            {
                ClearContext();
            }
        }

        [HarmonyPatch(typeof(CollectibleBehaviorQuenchable), nameof(CollectibleBehaviorQuenchable.GetShatterChance))]
        public static class QuenchShatterChancePatch
        {
            public static void Postfix(object[] __args, ref float __result)
            {
                if (CurrentQuenchBreakChancePerIteration <= 0 || __args.Length < 2 || __args[1] is not ItemStack itemstack)
                {
                    return;
                }

                __result = GetContextualShatterChance(itemstack, __result);
            }
        }

        [HarmonyPatch(typeof(CollectibleBehaviorQuenchable), "applyQuenchedStats")]
        public static class QuenchStoredShatterChancePatch
        {
            public static void Prefix(CollectibleBehaviorQuenchable __instance, IWorldAccessor world, ItemStack itemstack)
            {
                ClearQuenchApplyContext();

                if (CurrentQuenchBreakChancePerIteration <= 0)
                {
                    return;
                }

                CurrentShatterChanceBeforeQuench = GetContextualShatterChance(
                    itemstack,
                    itemstack.Attributes.GetFloat(ShatterChanceAttribute, __instance.BreakChancePerQuench)
                );
                HasCurrentShatterChanceBeforeQuench = true;
            }

            public static void Postfix(CollectibleBehaviorQuenchable __instance, IWorldAccessor world, ItemStack itemstack)
            {
                if (CurrentQuenchBreakChancePerIteration <= 0 || !HasCurrentShatterChanceBeforeQuench)
                {
                    return;
                }

                SetStoredShatterChance(
                    __instance,
                    world,
                    itemstack,
                    CurrentShatterChanceBeforeQuench + CurrentQuenchBreakChancePerIteration
                );
                ClearQuenchApplyContext();
            }
        }

        [HarmonyPatch(typeof(CollectibleBehaviorQuenchable), "applyTemperedStats")]
        public static class QuenchTemperedStatsPatch
        {
            public static void Postfix(ItemStack itemstack)
            {
                if (CurrentQuenchBreakChancePerIteration <= 0)
                {
                    return;
                }

                itemstack.Attributes.SetFloat(QuenchBreakChancePerIterationAttribute, CurrentQuenchBreakChancePerIteration);
            }
        }

        private static bool HasTrait(IPlayer player, string traitCode)
        {
            var charSystem = player.Entity.World.Api.ModLoader.GetModSystem<CharacterSystem>();
            if (charSystem == null)
            {
                return false;
            }

            string characterClassCode = player.Entity.WatchedAttributes.GetString("characterClass");
            var charClass = charSystem.characterClasses.Find(c => c.Code == characterClassCode);
            return charClass?.Traits.Contains(traitCode) == true;
        }

        private static float GetQuenchBreakChancePerIteration(ItemSlot? slot, IWorldAccessor? world = null)
        {
            IPlayer? player = null;

            if (slot?.Inventory is InventoryBasePlayer inventory)
            {
                player = inventory.Player;
            }

            player ??= GetClientWorldPlayer(world);

            if (player == null)
            {
                return 0;
            }

            return HasTrait(player, MetallurgistTrait)
                ? MetallurgistQuenchBreakChancePerIteration
                : DefaultQuenchBreakChancePerIteration;
        }

        private static IPlayer? GetClientWorldPlayer(IWorldAccessor? world)
        {
            PropertyInfo? playerProperty = world?.GetType().GetProperty("Player");
            return playerProperty?.GetValue(world) as IPlayer;
        }

        private static float GetContextualShatterChance(ItemStack itemstack, float storedChance)
        {
            float storedBreakChancePerIteration = itemstack.Attributes.GetFloat(
                QuenchBreakChancePerIterationAttribute,
                DefaultQuenchBreakChancePerIteration
            );

            if (storedBreakChancePerIteration <= 0)
            {
                return storedChance;
            }

            return storedChance * CurrentQuenchBreakChancePerIteration / storedBreakChancePerIteration;
        }

        private static void SetStoredShatterChance(CollectibleBehaviorQuenchable behavior, IWorldAccessor world, ItemStack itemstack, float shatterChance)
        {
            behavior.SetShatterChance(world, itemstack, shatterChance);
            itemstack.Attributes.SetFloat(QuenchBreakChancePerIterationAttribute, CurrentQuenchBreakChancePerIteration);
        }

        private static void ClearContext()
        {
            CurrentQuenchBreakChancePerIteration = 0;
            ClearQuenchApplyContext();
        }

        private static void ClearQuenchApplyContext()
        {
            CurrentShatterChanceBeforeQuench = 0;
            HasCurrentShatterChanceBeforeQuench = false;
        }
    }
}
