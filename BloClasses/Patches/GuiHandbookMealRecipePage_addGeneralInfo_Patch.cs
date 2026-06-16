using HarmonyLib;
using RPClasses.Extensions;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace RPClasses.Patches
{
    // This patch adds information about required trait for cooking recipes in the meal recipe handbook page.
    [HarmonyPatch(typeof(GuiHandbookMealRecipePage), "addGeneralInfo")]
    public static class GuiHandbookMealRecipePage_addGeneralInfo_Patch
    {
        public static void Postfix(GuiHandbookMealRecipePage __instance, ICoreClientAPI capi, ItemStack[] allStacks, List<RichTextComponentBase> components)
        {
            if (__instance.Recipe?.Code != null)
            {
                var requiresTrait = capi.GetCookingRecipeTraitRequirementByCookinRecipeCode(__instance.Recipe.Code);
                if (requiresTrait != null && requiresTrait.RequiresTrait != null)
                {
                    capi.Logger.Notification(
                        new System.Diagnostics.StackTrace().ToString()
                    );
                    components.AddRange(VtmlUtil.Richtextify(capi, Lang.Get("game:gridrecipe-requirestrait", Lang.Get($"rpclasses:traitname-{requiresTrait.RequiresTrait}")) + "\n", CairoFont.WhiteSmallText()));
                }
            }
        }
    }
}
