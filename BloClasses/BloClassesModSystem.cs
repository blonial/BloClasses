using BloClasses.EntityBehaviors;
using HarmonyLib;
using RPClasses.BlockEntities;
using RPClasses.Blocks;
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
        }
    }
}
