using BloClasses.Extensions;
using System;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace BloClasses.Blocks
{
    public class CustomBlockCookingContainer : BlockCookingContainer
    {
        public const string LastTouchedPlayerTraitsAttribute = "bloclassesLastTouchedPlayerTraits";
        public const string CookedFoodSaturationMultiplierAttribute = "bloclassesCookedFoodSaturationMul";
        public const string CookedFoodPerishTimeMultiplierAttribute = "bloclassesCookedFoodPerishTimeMul";

        private static readonly FieldInfo? MaxServingSizeField = FindInstanceField(typeof(CustomBlockCookingContainer), "maxServingSize");

        public IPlayer? LastTouchingPlayer;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            var maxServingSize = Attributes?["maxServingSize"].AsInt(0) ?? 0;
            if (maxServingSize <= 0)
            {
                maxServingSize = Attributes?["servingCapacity"].AsInt(0) ?? 0;
            }
            if (maxServingSize <= 0)
            {
                maxServingSize = Attributes?["maxContainerSlotStackSize"].AsInt(0) ?? 0;
            }

            if (maxServingSize > 0)
            {
                MaxServingSizeField?.SetValue(this, maxServingSize);
            }
        }

        private static FieldInfo? FindInstanceField(Type? type, string fieldName)
        {
            while (type != null)
            {
                var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    return field;
                }

                type = type.BaseType;
            }

            return null;
        }

        public override bool CanSmelt(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemStack inputStack, ItemStack outputStack)
        {
            if (!DoesPlayerMeetTraitRequirement(world, cookingSlotsProvider, inputStack))
            {
                return false;
            }

            return base.CanSmelt(world, cookingSlotsProvider, inputStack, outputStack);
        }


        public new string? GetOutputText(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot)
        {
            if (inputSlot.Itemstack == null)
            {
                return null;
            }

            if (!(inputSlot.Itemstack.Collectible is BlockCookingContainer))
            {
                return null;
            }

            if (!DoesPlayerMeetTraitRequirement(world, cookingSlotsProvider, inputSlot.Itemstack))
            {
                foreach (var item in cookingSlotsProvider.Slots)
                {
                    if (item.GetStackName() != null)
                    {
                        return Lang.Get("mealcreation-norecipe");
                    }
                }
                return null;
            }

            return base.GetOutputText(world, cookingSlotsProvider, inputSlot);
        }

        public static void RememberPlayerOnStack(ItemStack? itemStack, IPlayer player)
        {
            if (itemStack?.Collectible is not CustomBlockCookingContainer cookingBlock)
            {
                return;
            }

            cookingBlock.LastTouchingPlayer = player;

            var charSystem = player.Entity.World.Api.ModLoader.GetModSystem<CharacterSystem>();
            string characterClassCode = player.Entity.WatchedAttributes.GetString("characterClass");
            var charClass = charSystem?.characterClasses.Find(c => c.Code == characterClassCode);
            string[] traits = charClass?.Traits?.ToArray() ?? Array.Empty<string>();

            itemStack.Attributes.SetString(LastTouchedPlayerTraitsAttribute, string.Join("|", traits));
            SetOrRemoveFloat(itemStack, CookedFoodSaturationMultiplierAttribute, player.Entity.Stats.GetBlended("cookedFoodSaturation"));
            SetOrRemoveFloat(itemStack, CookedFoodPerishTimeMultiplierAttribute, GetTimeMultiplierFromStat(player.Entity.Stats.GetBlended("cookedFoodPerishTime")));
        }

        private static void SetOrRemoveFloat(ItemStack itemStack, string attribute, float value)
        {
            if (value == 0f || value == 1f)
            {
                itemStack.Attributes.RemoveAttribute(attribute);
                return;
            }

            itemStack.Attributes.SetFloat(attribute, value);
        }

        private static float GetTimeMultiplierFromStat(float statValue)
        {
            if (statValue == 0f || statValue == 1f)
            {
                return 1f;
            }

            return statValue < 1f ? 1f + (1f - statValue) : statValue;
        }

        private bool DoesPlayerMeetTraitRequirement(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemStack inputStack)
        {
            var cookingRecipe = GetMatchingCookingRecipe(world, GetCookingStacks(cookingSlotsProvider, clone: false), out var quantityServings);
            if (cookingRecipe != null && cookingRecipe.Code != null)
            {
                var cookingRecipesTraitRequirement = world.Api.GetCookingRecipeTraitRequirementByCookinRecipeCode(cookingRecipe.Code);
                if (cookingRecipesTraitRequirement != null && cookingRecipesTraitRequirement.RequiresTrait != null)
                {
                    if (LastTouchingPlayer == null)
                    {
                        return StackHasTrait(inputStack, cookingRecipesTraitRequirement.RequiresTrait);
                    }

                    return TraitRequirementUtil.PlayerHasTrait(LastTouchingPlayer, cookingRecipesTraitRequirement.RequiresTrait);
                }
            }

            return true;
        }

        private static bool StackHasTrait(ItemStack itemStack, string requiredTrait)
        {
            string traits = itemStack.Attributes.GetString(LastTouchedPlayerTraitsAttribute, string.Empty);
            return traits
                .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Contains(requiredTrait);
        }
    }
}
