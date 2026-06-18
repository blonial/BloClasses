using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace BloClasses.Patches
{
    [HarmonyPatch(typeof(PModulePlayerInLiquid))]
    public static class SwimmingSpeed_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(PModulePlayerInLiquid.HandleSwimming))]
        public static void HandleSwimmingPrefix(EntityPos pos, out MotionState __state)
        {
            __state = new MotionState(pos.Motion.X, pos.Motion.Y, pos.Motion.Z);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(PModulePlayerInLiquid.HandleSwimming))]
        public static void HandleSwimmingPostfix(Entity entity, EntityPos pos, MotionState __state)
        {
            float modifier = entity.Stats.GetBlended("swimmingSpeed");
            if (modifier <= 0 || modifier == 1)
            {
                return;
            }

            pos.Motion.X = __state.X + (pos.Motion.X - __state.X) * modifier;
            pos.Motion.Y = __state.Y + (pos.Motion.Y - __state.Y) * modifier;
            pos.Motion.Z = __state.Z + (pos.Motion.Z - __state.Z) * modifier;
        }

        public readonly struct MotionState
        {
            public MotionState(double x, double y, double z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            public double X { get; }
            public double Y { get; }
            public double Z { get; }
        }
    }
}
