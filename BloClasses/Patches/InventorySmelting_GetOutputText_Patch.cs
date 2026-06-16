using HarmonyLib;
using RPClasses.Blocks;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace RPClasses.Patches
{
    // This patch overrides default output text displayed in firepit if player does not have required trait for cooking recipe with custom text provided by CustomBlockCookingContainer
    // Original method InventorySmelting.GetOutputText uses BlockCookingContainer.GetOutputText and it is not virual so we have to override it
    [HarmonyPatch(typeof(InventorySmelting), nameof(InventorySmelting.GetOutputText))]
    public static class InventorySmelting_GetOutputText_Patch
    {
        public static bool Prefix(InventorySmelting __instance, ref string? __result)
        {
            ItemStack? itemstack = __instance[1].Itemstack;

            if (itemstack?.Collectible is not CustomBlockCookingContainer customContainer)
            {
                return true;
            }

            __result = customContainer.GetOutputText(
                __instance.Api.World,
                __instance,
                __instance[1]
            );

            return false;
        }
    }
}
