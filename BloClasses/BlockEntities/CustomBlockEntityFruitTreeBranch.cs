using System.Text;
using BloClasses.Patches;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace BloClasses.BlockEntities
{
    public class CustomBlockEntityFruitTreeBranch : BlockEntityFruitTreeBranch
    {
        public float FruitTreeCuttingPlaceFailure { get; set; }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            FruitTreeCuttingPlaceFailure = tree.GetFloat(FruitTreeCuttingAttributeNames.PlaceFailure);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            if (FruitTreeCuttingPlaceFailure == 0)
            {
                tree.RemoveAttribute(FruitTreeCuttingAttributeNames.PlaceFailure);
                return;
            }

            tree.SetFloat(FruitTreeCuttingAttributeNames.PlaceFailure, FruitTreeCuttingPlaceFailure);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);

            if (FruitTreeCuttingPlaceFailure == 0)
            {
                return;
            }

            dsc.AppendLine(Lang.Get(
                "bloclasses:fruittreecutting-survivalchance-modifier",
                FormatSignedPercent(-FruitTreeCuttingPlaceFailure)
            ));
        }

        private static string FormatSignedPercent(float value)
        {
            return $"{100f * value:+0.#;-0.#;0}%";
        }
    }
}
