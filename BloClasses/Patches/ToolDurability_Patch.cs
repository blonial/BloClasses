using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace BloClasses.Patches
{
    public static class ToolDurability_Patch
    {
        private const string ToolDurabilityMultiplierAttribute = "bloclassesToolDurabilityMul";
        private const string ToolDurabilityMaxMultiplierAttribute = "bloclassesToolDurabilityMaxMul";

        [ThreadStatic]
        private static IPlayer? CurrentAnvilPlayer;

        [HarmonyPatch(typeof(BlockEntityAnvil), "CheckIfFinished")]
        public static class ForgedToolHeadPatch
        {
            public static void Prefix(BlockEntityAnvil __instance, IPlayer byPlayer)
            {
                CurrentAnvilPlayer = byPlayer;
            }

            public static void Finalizer()
            {
                CurrentAnvilPlayer = null;
            }
        }

        [HarmonyPatch]
        public static class GiveForgedToolHeadPatch
        {
            public static IEnumerable<MethodBase> TargetMethods()
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type[] types;
                    try
                    {
                        types = assembly.GetTypes();
                    }
                    catch (ReflectionTypeLoadException exception)
                    {
                        types = exception.Types.Where(type => type != null).Cast<Type>().ToArray();
                    }

                    foreach (Type type in types)
                    {
                        if (type.IsInterface || type.IsAbstract)
                        {
                            continue;
                        }

                        foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                        {
                            ParameterInfo[] parameters = method.GetParameters();
                            if (
                                !method.IsAbstract
                                && method.Name == "TryGiveItemstack"
                                && parameters.Length > 0
                                && parameters[0].ParameterType == typeof(ItemStack)
                            )
                            {
                                yield return method;
                            }
                        }
                    }
                }
            }

            public static void Prefix(object[] __args)
            {
                if (CurrentAnvilPlayer == null || __args.Length == 0 || __args[0] is not ItemStack itemStack)
                {
                    return;
                }

                ApplyForgedToolHeadDurability(itemStack, CurrentAnvilPlayer);
            }
        }

        [HarmonyPatch]
        public static class CraftedToolPatch
        {
            public static IEnumerable<MethodBase> TargetMethods()
            {
                Type itemSlotCraftingOutputType = AccessTools.TypeByName("Vintagestory.Common.ItemSlotCraftingOutput");
                if (itemSlotCraftingOutputType == null)
                {
                    yield break;
                }

                MethodInfo craftSingleMethod = AccessTools.Method(itemSlotCraftingOutputType, "CraftSingle");
                if (craftSingleMethod != null)
                {
                    yield return craftSingleMethod;
                }

                MethodInfo craftManyMethod = AccessTools.Method(itemSlotCraftingOutputType, "CraftMany");
                if (craftManyMethod != null)
                {
                    yield return craftManyMethod;
                }
            }

            public static void Prefix(object __instance)
            {
                ItemStack? itemStack = Traverse.Create(__instance).Property<ItemStack>("Itemstack").Value;
                ApplyCopiedToolHeadDurability(itemStack, __instance);
            }
        }

        [HarmonyPatch]
        public static class CraftingOutputPreviewPatch
        {
            public static MethodBase? TargetMethod()
            {
                Type inventoryCraftingGridType = AccessTools.TypeByName("Vintagestory.Common.InventoryCraftingGrid");
                return inventoryCraftingGridType == null
                    ? null
                    : AccessTools.Method(inventoryCraftingGridType, "FoundMatch", new[] { typeof(GridRecipe) });
            }

            public static void Postfix(object __instance)
            {
                object? outputSlot = Traverse.Create(__instance).Field("outputSlot").GetValue();
                if (outputSlot == null)
                {
                    return;
                }

                ItemStack? itemStack = Traverse.Create(outputSlot).Property<ItemStack>("Itemstack").Value;
                ApplyCopiedToolHeadDurability(itemStack, outputSlot);
            }
        }

        public static void ApplyForgedToolHeadDurability(ItemStack workItemStack, IPlayer player)
        {
            if (player?.Entity == null || !IsToolHead(workItemStack))
            {
                return;
            }

            float modifier = player.Entity.Stats.GetBlended("toolDurability");
            if (modifier == 0 || modifier == 1)
            {
                return;
            }

            float durabilityMultiplier = modifier > 1f ? modifier : 1f + modifier;
            workItemStack.Attributes.SetFloat(ToolDurabilityMultiplierAttribute, durabilityMultiplier);
        }

        public static void ApplyCopiedToolHeadDurability(ItemStack? itemStack, object craftingOutputSlot)
        {
            if (itemStack == null)
            {
                return;
            }

            float outputModifier = itemStack.Attributes.GetFloat(ToolDurabilityMultiplierAttribute, 1f);
            float inputModifier = GetBestToolHeadDurabilityModifier(craftingOutputSlot);
            float modifier = inputModifier != 1f ? inputModifier : outputModifier;
            float currentMaxModifier = itemStack.Attributes.GetFloat(ToolDurabilityMaxMultiplierAttribute, 1f);

            if (IsToolHead(itemStack) && inputModifier != 1f)
            {
                itemStack.Attributes.SetFloat(ToolDurabilityMultiplierAttribute, inputModifier);
                return;
            }

            if (itemStack.Collectible.Durability <= 0)
            {
                return;
            }

            if (modifier <= 0 || modifier == 1)
            {
                return;
            }

            if (currentMaxModifier == modifier)
            {
                return;
            }

            int remainingDurability = itemStack.Collectible.GetRemainingDurability(itemStack);
            int boostedDurability = Math.Max(1, (int)MathF.Round(remainingDurability * modifier));

            itemStack.Attributes.SetInt("durability", boostedDurability);
            itemStack.Attributes.SetFloat(ToolDurabilityMaxMultiplierAttribute, modifier);
            itemStack.Attributes.RemoveAttribute(ToolDurabilityMultiplierAttribute);
        }

        [HarmonyPatch(typeof(CollectibleObject), nameof(CollectibleObject.GetMaxDurability), typeof(ItemStack))]
        public static class MaxToolDurabilityPatch
        {
            public static void Postfix(ItemStack itemstack, ref int __result)
            {
                float modifier = itemstack?.Attributes.GetFloat(ToolDurabilityMaxMultiplierAttribute, 1f) ?? 1f;
                if (modifier <= 0 || modifier == 1f)
                {
                    return;
                }

                __result = Math.Max(1, (int)MathF.Round(__result * modifier));
            }
        }

        [HarmonyPatch(typeof(CollectibleObject), nameof(CollectibleObject.GetHeldItemInfo), typeof(ItemSlot), typeof(StringBuilder), typeof(IWorldAccessor), typeof(bool))]
        public static class ToolHeadInfoPatch
        {
            public static void Postfix(ItemSlot inSlot, StringBuilder dsc)
            {
                ItemStack? itemStack = inSlot?.Itemstack;
                if (itemStack == null || !IsToolHead(itemStack) || !itemStack.Attributes.HasAttribute(ToolDurabilityMultiplierAttribute))
                {
                    return;
                }

                dsc.AppendLine(Lang.Get("bloclasses:toolhead-quality-high"));
            }
        }

        private static float GetBestToolHeadDurabilityModifier(object craftingOutputSlot)
        {
            var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
            return GetBestToolHeadDurabilityModifier(craftingOutputSlot, 0, visited);
        }

        private static float GetBestToolHeadDurabilityModifier(object? value, int depth, HashSet<object> visited)
        {
            if (value == null || value is string || value.GetType().IsPrimitive || value.GetType().IsEnum || depth > 4)
            {
                return 1f;
            }

            if (!visited.Add(value))
            {
                return 1f;
            }

            if (value is ItemSlot slot)
            {
                float slotBestModifier = GetToolHeadDurabilityModifier(slot.Itemstack);

                object? inventory = Traverse.Create(slot).Property<object>("Inventory").Value;
                if (inventory != null)
                {
                    slotBestModifier = Math.Max(slotBestModifier, GetBestToolHeadDurabilityModifier(inventory, depth + 1, visited));
                }

                foreach (FieldInfo field in GetAllInstanceFields(slot.GetType()))
                {
                    object? fieldValue = field.GetValue(slot);
                    slotBestModifier = Math.Max(slotBestModifier, GetBestToolHeadDurabilityModifier(fieldValue, depth + 1, visited));
                }

                return slotBestModifier;
            }

            if (value is ItemStack itemStack)
            {
                return GetToolHeadDurabilityModifier(itemStack);
            }

            float bestModifier = 1f;

            if (value is IEnumerable enumerable)
            {
                int index = 0;
                foreach (object? item in enumerable)
                {
                    bestModifier = Math.Max(bestModifier, GetBestToolHeadDurabilityModifier(item, depth + 1, visited));
                    index++;
                }

                return bestModifier;
            }

            foreach (FieldInfo field in GetAllInstanceFields(value.GetType()))
            {
                object? fieldValue = field.GetValue(value);
                float modifier = GetBestToolHeadDurabilityModifier(fieldValue, depth + 1, visited);
                bestModifier = Math.Max(bestModifier, modifier);
            }

            return bestModifier;
        }

        private static IEnumerable<FieldInfo> GetAllInstanceFields(Type type)
        {
            for (Type? currentType = type; currentType != null && currentType != typeof(object); currentType = currentType.BaseType)
            {
                foreach (FieldInfo field in currentType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    yield return field;
                }
            }
        }

        private static float GetToolHeadDurabilityModifier(ItemStack? itemStack)
        {
            if (itemStack == null || !itemStack.Attributes.HasAttribute(ToolDurabilityMultiplierAttribute))
            {
                return 1f;
            }

            return itemStack.Attributes.GetFloat(ToolDurabilityMultiplierAttribute, 1f);
        }

        private static bool IsToolHead(ItemStack itemStack)
        {
            return itemStack.Collectible.Code.Path.Contains("head");
        }
    }
}
