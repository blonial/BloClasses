using HarmonyLib;
using RPClasses.Blocks;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace RPClasses.Patches
{
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
