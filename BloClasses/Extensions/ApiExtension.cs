using RPClasses.RecipeRegistrySystems;
using RPClasses.RegistrySystems;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace RPClasses.Extensions
{
    public static class ApiExtension
    {
        public static List<CookingRecipeRequiresTrait> GetCookingRecipesTraitRequirements(this ICoreAPI api)
        {
            return api.ModLoader.GetModSystem<CookingRecipesTraitRequirementRegistrySystem>().CookingRecipesTraitRequirements;
        }

        public static CookingRecipeRequiresTrait? GetCookingRecipeTraitRequirementByCookinRecipeCode(this ICoreAPI api, string cookingRecipeCode)
        {
            return api.ModLoader.GetModSystem<CookingRecipesTraitRequirementRegistrySystem>().CookingRecipesTraitRequirements.Find(c => c.Code == cookingRecipeCode);
        }
    }
}
