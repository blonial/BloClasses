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

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            LastTouchingPlayer = byPlayer;
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            LastTouchingPlayer = byPlayer;
            return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
        }

        public override bool CanSmelt(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemStack inputStack, ItemStack outputStack)
        {
            if (!DoesPlayerMeetTraitRequirement(world, cookingSlotsProvider))
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

            if (!DoesPlayerMeetTraitRequirement(world, cookingSlotsProvider))
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

        private bool DoesPlayerMeetTraitRequirement(IWorldAccessor world, ISlotProvider cookingSlotsProvider)
        {
            if (LastTouchingPlayer == null)
            {
                return false;
            }

            var cookingRecipe = GetMatchingCookingRecipe(world, GetCookingStacks(cookingSlotsProvider, clone: false), out var quantityServings);
            if (cookingRecipe != null && cookingRecipe.Code != null)
            {
                var cookingRecipesTraitRequirement = world.Api.GetCookingRecipeTraitRequirementByCookinRecipeCode(cookingRecipe.Code);
                if (cookingRecipesTraitRequirement != null && cookingRecipesTraitRequirement.RequiresTrait != null)
                {
                    return TraitRequirementUtil.PlayerHasTrait(LastTouchingPlayer, cookingRecipesTraitRequirement.RequiresTrait);
                }
            }

            return true;
        }
    }
}
