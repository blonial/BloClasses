using RPClasses.Blocks;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace RPClasses.BlockEntities
{
    public class CustomBlockEntityFirepit : BlockEntityFirepit
    {
        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (inputStack != null && inputStack.Collectible is CustomBlockCookingContainer)
            {
                var cookingBlock = (CustomBlockCookingContainer)inputStack.Collectible;
                cookingBlock.LastTouchingPlayer = byPlayer;
            }

            return base.OnPlayerRightClick(byPlayer, blockSel);
        }
    }
}
