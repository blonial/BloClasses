using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace BloClasses.Patches
{
    [HarmonyPatch(typeof(BlockPan), "CreateDrop")]
    public static class PanningDrop_Patch
    {
        private static readonly HashSet<string> QualityDropCodes = new()
        {
            "gear-temporal",
            "nugget-nativegold",
            "nugget-nativesilver",
            "nugget-cassiterite",
            "ore-lapislazuli",
            "metal-parts",
            "metal-scraps"
        };

        private static readonly string[] QualityDropPrefixes =
        {
            "gem-",
            "tuningcylinder-",
            "metallamellae-",
            "metalchain-",
            "clothes-neck-",
            "clothes-arm-",
            "clothes-head-"
        };

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var chanceField = AccessTools.Field(typeof(PanningDrop), nameof(PanningDrop.Chance));
            var nextFloatMethod = AccessTools.Method(typeof(NatFloat), nameof(NatFloat.nextFloat));
            var applyModifierMethod = AccessTools.Method(typeof(PanningDrop_Patch), nameof(ApplyQualityDropModifier));

            for (int i = 1; i < codes.Count - 4; i++)
            {
                if (!codes[i].LoadsField(chanceField) || !codes[i + 1].Calls(nextFloatMethod))
                {
                    continue;
                }

                if (codes[i + 3].opcode != OpCodes.Mul || !codes[i + 4].IsStloc())
                {
                    continue;
                }

                object dropLocal = codes[i - 1].operand;
                object chanceLocal = codes[i + 4].operand;

                codes.InsertRange(i + 5, new[]
                {
                    new CodeInstruction(OpCodes.Ldloc_S, chanceLocal),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldloc_S, dropLocal),
                    new CodeInstruction(OpCodes.Call, applyModifierMethod),
                    new CodeInstruction(OpCodes.Stloc_S, chanceLocal)
                });

                return codes;
            }

            return codes;
        }

        private static float ApplyQualityDropModifier(float chance, EntityAgent entity, PanningDrop drop)
        {
            bool isQualityDrop = IsQualityDrop(drop);
            float modifier = entity.Stats.GetBlended("panningDropQualityMod");
            float finalChance = isQualityDrop ? chance * GameMath.Max(0, modifier) : chance;

            entity.World.Logger.Notification(
                "[BloClasses/PanningDrop] Drop chance: code={0}, manMade={1}, quality={2}, baseChance={3}, modifier={4}, finalChance={5}",
                GetDropCode(drop),
                drop.ManMade,
                isQualityDrop,
                chance,
                modifier,
                finalChance
            );

            if (!isQualityDrop)
            {
                return chance;
            }

            return finalChance;
        }

        private static bool IsQualityDrop(PanningDrop drop)
        {
            if (drop.ManMade)
            {
                return true;
            }

            string? code = drop.Code?.Path;
            if (code == null)
            {
                return false;
            }

            if (QualityDropCodes.Contains(code))
            {
                return true;
            }

            foreach (string prefix in QualityDropPrefixes)
            {
                if (code.StartsWith(prefix))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetDropCode(PanningDrop drop)
        {
            return drop.Code?.Path ?? "<unknown>";
        }
    }
}
