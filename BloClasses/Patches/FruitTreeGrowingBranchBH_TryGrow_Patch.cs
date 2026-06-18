using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BloClasses.BlockEntities;
using HarmonyLib;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace BloClasses.Patches
{
    [HarmonyPatch(typeof(FruitTreeGrowingBranchBH))]
    public static class FruitTreeGrowingBranchBHPatches
    {
        private static readonly MethodInfo? OwnBeGetter = AccessTools.PropertyGetter(typeof(FruitTreeGrowingBranchBH), "ownBe");

        [HarmonyTranspiler]
        [HarmonyPatch("TryGrow")]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo rootingChanceField = AccessTools.Field(typeof(FruitTreeTypeProperties), nameof(FruitTreeTypeProperties.CuttingRootingChance));
            FieldInfo graftChanceField = AccessTools.Field(typeof(FruitTreeTypeProperties), nameof(FruitTreeTypeProperties.CuttingGraftChance));
            MethodInfo applyModifierMethod = AccessTools.Method(typeof(FruitTreeGrowingBranchBHPatches), nameof(ApplyFruitTreeCuttingPlaceFailure));

            foreach (CodeInstruction instruction in instructions)
            {
                yield return instruction;

                if (instruction.opcode == OpCodes.Ldfld && (instruction.operand.Equals(rootingChanceField) || instruction.operand.Equals(graftChanceField)))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, applyModifierMethod);
                }
            }
        }

        public static float ApplyFruitTreeCuttingPlaceFailure(float chance, FruitTreeGrowingBranchBH behavior)
        {
            float modifier = GetFruitTreeCuttingPlaceFailure(behavior);
            return modifier == 0 ? chance : GameMath.Clamp(chance - modifier, 0, 1);
        }

        private static float GetFruitTreeCuttingPlaceFailure(FruitTreeGrowingBranchBH behavior)
        {
            var be = OwnBeGetter?.Invoke(behavior, null) as CustomBlockEntityFruitTreeBranch;
            return be?.FruitTreeCuttingPlaceFailure ?? 0;
        }
    }
}
