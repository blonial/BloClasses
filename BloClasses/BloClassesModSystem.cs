using BloClasses.EntityBehaviors;
using HarmonyLib;
using BloClasses.BlockEntities;
using BloClasses.Blocks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace BloClasses
{
    public class BloClassesModSystem : ModSystem
    {

        private static Harmony? Patcher;

        public override void Start(ICoreAPI api)
        {
            api.RegisterBlockClass("CustomBlockCookingContainer", typeof(CustomBlockCookingContainer));

            api.RegisterBlockEntityClass("CustomBlockEntityFirepit", typeof(CustomBlockEntityFirepit));
            api.RegisterBlockEntityClass("CustomBlockEntityFruitTreeBranch", typeof(CustomBlockEntityFruitTreeBranch));

            api.RegisterEntityBehaviorClass("UndergroundStabilityLossModEntityBehavior", typeof(UndergroundStabilityLossModEntityBehavior));

            if (Patcher == null)
            {
                Patcher = new Harmony(Mod.Info.ModID);
                Patcher.PatchAll();
            }
        }

        public override void Dispose()
        {
            Patcher?.UnpatchAll(Mod.Info.ModID);
            Patcher = null;
        }
    }
}
