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
            ItemStack?[] beforeStacks = SnapshotInventory();

            base.OnReceivedClientPacket(player, packetid, data);

            if (InventoryChanged(beforeStacks))
            {
                RememberPlayerForCookingContainer(player);
            }
        }

        private ItemStack?[] SnapshotInventory()
        {
            var snapshot = new ItemStack?[Inventory.Count];

            for (int i = 0; i < Inventory.Count; i++)
            {
                snapshot[i] = Inventory[i].Itemstack?.Clone();
            }

            return snapshot;
        }

        private bool InventoryChanged(ItemStack?[] beforeStacks)
        {
            if (beforeStacks.Length != Inventory.Count)
            {
                return true;
            }

            for (int i = 0; i < Inventory.Count; i++)
            {
                if (!StacksEqual(beforeStacks[i], Inventory[i].Itemstack))
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
