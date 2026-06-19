using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BloClasses.Extensions;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace BloClasses.Patches
{
    [HarmonyPatch(typeof(CollectibleBehaviorHandbookTextAndExtraInfo), "addGeneralInfo")]
    public static class CollectibleBehaviorHandbookTextAndExtraInfo_AddGeneralInfo_Patch
    {
        public static void Postfix(ItemSlot inSlot, ICoreClientAPI capi, List<RichTextComponentBase> components)
        {
            string? stackCode = inSlot?.Itemstack?.Collectible?.Code?.ToString();
            if (stackCode == null)
            {
                return;
            }

            string? requiredTrait = capi.GetClayFormingRecipesTraitRequirements()
                .FirstOrDefault(requirement => MatchesOutputCode(requirement.OutputCode, stackCode))
                ?.RequiresTrait;
            if (requiredTrait == null)
            {
                return;
            }

            components.AddRange(VtmlUtil.Richtextify(
                capi,
                Lang.Get("game:gridrecipe-requirestrait", Lang.Get($"game:traitname-{requiredTrait}")) + "\n",
                CairoFont.WhiteSmallText()
            ));
        }

        private static bool MatchesOutputCode(string? recipeOutputCode, string stackCode)
        {
            if (recipeOutputCode == null)
            {
                return false;
            }

            return MatchesNormalizedOutputCode(recipeOutputCode, stackCode)
                || MatchesNormalizedOutputCode(WithoutDomain(recipeOutputCode), WithoutDomain(stackCode));
        }

        private static bool MatchesNormalizedOutputCode(string recipeOutputCode, string stackCode)
        {
            string wildcard = recipeOutputCode.Replace("{color}", "*");
            string pattern = "^" + Regex.Escape(wildcard).Replace("\\*", ".*") + "$";
            return Regex.IsMatch(stackCode, pattern);
        }

        private static string WithoutDomain(string code)
        {
            int separatorIndex = code.IndexOf(':');
            return separatorIndex < 0 ? code : code[(separatorIndex + 1)..];
        }
    }
}
