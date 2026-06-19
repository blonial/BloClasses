using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace BloClasses.BlockEntities
{
    public class CustomBlockEntityBarrel : BlockEntityBarrel
    {
        private const string RichAlcoholProductionAttribute = "bloClassesRichAlcoholProduction";

        public bool RichAlcoholProduction { get; set; }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            RichAlcoholProduction = tree.GetBool(RichAlcoholProductionAttribute);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBool(RichAlcoholProductionAttribute, RichAlcoholProduction);
        }
    }
}
