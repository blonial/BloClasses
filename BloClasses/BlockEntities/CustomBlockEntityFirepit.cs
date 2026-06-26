using BloClasses.Blocks;
using System;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace BloClasses.BlockEntities
{
    public class CustomBlockEntityFirepit : BlockEntityFirepit
    {
        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            ItemStack?[] beforeStacks = SnapshotCookingSlots();

            base.OnReceivedClientPacket(player, packetid, data);

            if (CookingSlotsChanged(beforeStacks))
            {
                RememberPlayerForCookingContainer(player);
            }
        }

        private ItemStack?[] SnapshotCookingSlots()
        {
            var snapshot = new ItemStack?[otherCookingSlots.Length];

            for (int i = 0; i < otherCookingSlots.Length; i++)
            {
                snapshot[i] = otherCookingSlots[i].Itemstack?.Clone();
            }

            return snapshot;
        }

        private bool CookingSlotsChanged(ItemStack?[] beforeStacks)
        {
            if (beforeStacks.Length != otherCookingSlots.Length)
            {
                return true;
            }

            for (int i = 0; i < otherCookingSlots.Length; i++)
            {
                if (!StacksEqual(beforeStacks[i], otherCookingSlots[i].Itemstack))
                {
                    return true;
                }
            }

            return false;
        }

        private bool StacksEqual(ItemStack? beforeStack, ItemStack? afterStack)
        {
            if (beforeStack == null || afterStack == null)
            {
                return beforeStack == afterStack;
            }

            return beforeStack.StackSize == afterStack.StackSize
                && afterStack.Equals(Api.World, beforeStack, Array.Empty<string>());
        }

        private void RememberPlayerForCookingContainer(IPlayer player)
        {
            if (inputStack?.Collectible is CustomBlockCookingContainer)
            {
                CustomBlockCookingContainer.RememberPlayerOnStack(inputStack, player);
                inputSlot.MarkDirty();
            }
        }
    }
}
