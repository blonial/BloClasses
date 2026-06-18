using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace BloClasses.Patches
{
    [HarmonyPatch(typeof(BlockCrop), nameof(BlockCrop.GetDrops))]
    public static class SuperPlants_Patch
    {
        public static void Postfix(ref ItemStack[] __result, IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            var charSystem = world.Api.ModLoader.GetModSystem<CharacterSystem>();
            if (charSystem == null)
            {
                return;
            }

            var charClass = charSystem.characterClasses.Find(c => c.Code == byPlayer.Entity.WatchedAttributes.GetString("characterClass"));
            if (charClass == null || !charClass.Traits.Contains("bcsuperplants"))
            {
                return;
            }

            var extraDrops = new List<ItemStack>();

            foreach (var stack in __result)
            {
                if (stack.Collectible.Code.ToString().Contains("game:seeds"))
                {
                    var val = world.Rand.NextDouble();

                    if (world.Rand.NextDouble() <= 0.03)
                    {
                        extraDrops.Add(
                            new ItemStack(
                                world.GetItem(
                                    new AssetLocation(
                                        stack.Collectible.Code.ToString().Replace("game", "bloclasses")
                                    )
                                )
                            )
                        );
                    }
                }
            }

            if (extraDrops.Count > 0)
            {
                __result = __result.Concat(extraDrops).ToArray();
            }
        }
    }
}
