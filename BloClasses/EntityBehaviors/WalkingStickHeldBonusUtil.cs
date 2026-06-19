using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace BloClasses.EntityBehaviors
{
    public static class WalkingStickHeldBonusUtil
    {
        private const string CowWalkingStickCode = "bloclasses:walkingstick-cowskull";

        public static bool PlayerHoldsCowWalkingStick(EntityPlayer player)
        {
            IPlayer? worldPlayer = player.World.PlayerByUid(player.PlayerUID);
            if (worldPlayer == null)
            {
                return false;
            }

            return IsCowWalkingStick(worldPlayer.InventoryManager.ActiveHotbarSlot)
                   || IsCowWalkingStick(worldPlayer.InventoryManager.OffhandHotbarSlot);
        }

        private static bool IsCowWalkingStick(ItemSlot? slot)
        {
            return slot?.Itemstack?.Collectible?.Code?.ToString() == CowWalkingStickCode;
        }
    }
}
