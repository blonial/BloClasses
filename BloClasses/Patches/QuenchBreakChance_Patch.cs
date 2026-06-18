using System;
using System.Text;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace BloClasses.Patches
{
    public static class QuenchBreakChance_Patch
    {
        private const float MetallurgistQuenchBreakChancePerIteration = 0.04f;
        private const float DefaultQuenchBreakChancePerIteration = 0.05f;
        private const string MetallurgistTrait = "bcmetallurgist";

        [ThreadStatic]
        private static float CurrentQuenchBreakChancePerIteration;

        [HarmonyPatch(typeof(CollectibleBehaviorQuenchable), "CoolToTemperature")]
        public static class QuenchCoolingContextPatch
        {
            public static void Prefix(object[] __args)
            {
                CurrentQuenchBreakChancePerIteration = GetQuenchBreakChancePerIteration(
                    __args.Length > 1 ? __args[1] as ItemSlot : null
                );
            }

            public static void Finalizer()
            {
                CurrentQuenchBreakChancePerIteration = 0;
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

                float storedChance = itemstack.Attributes.GetFloat("shatterchance", DefaultQuenchBreakChancePerIteration);
                string storedLine = Lang.Get("quenchable-shatter-chance", storedChance);
                string contextualLine = Lang.Get("quenchable-shatter-chance", GetContextualShatterChance(itemstack));

                dsc.Replace(storedLine, contextualLine);
            }

            public static void Finalizer()
            {
                CurrentQuenchBreakChancePerIteration = 0;
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

                __result = GetContextualShatterChance(itemstack);
            }
        }

        [HarmonyPatch(typeof(CollectibleBehaviorQuenchable), "applyQuenchedStats")]
        public static class QuenchStoredShatterChancePatch
        {
            public static void Postfix(CollectibleBehaviorQuenchable __instance, IWorldAccessor world, ItemStack itemstack)
            {
                if (CurrentQuenchBreakChancePerIteration <= 0)
                {
                    return;
                }

                __instance.SetShatterChance(world, itemstack, GetContextualShatterChance(itemstack));
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

            if (player == null && world is IClientWorldAccessor clientWorld)
            {
                player = clientWorld.Player;
            }

            if (player == null)
            {
                return 0;
            }

            return HasTrait(player, MetallurgistTrait)
                ? MetallurgistQuenchBreakChancePerIteration
                : DefaultQuenchBreakChancePerIteration;
        }

        private static float GetContextualShatterChance(ItemStack itemstack)
        {
            int quenchIteration = itemstack.Attributes.GetInt("quenchIteration", 0);
            return (quenchIteration + 1) * CurrentQuenchBreakChancePerIteration;
        }
    }
}
