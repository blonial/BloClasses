using System.Reflection;
using HarmonyLib;
using Vintagestory.GameContent;

namespace BloClasses.Patches
{
    [HarmonyPatch(typeof(EntityBehaviorBodyTemperature), "updateBodyTemperature")]
    public static class FreezeResistance_Patch
    {
        private const float BaseDamagingTemperatureDrop = 4f;

        private static readonly FieldInfo NormalBodyTemperatureField =
            AccessTools.Field(typeof(EntityBehaviorBodyTemperature), "NormalBodyTemperature");

        private static readonly FieldInfo DamagingFreezeHoursField =
            AccessTools.Field(typeof(EntityBehaviorBodyTemperature), "damagingFreezeHours");

        private static readonly FieldInfo EntityAgentField =
            AccessTools.Field(typeof(EntityBehaviorBodyTemperature), "eagent");

        [HarmonyPostfix]
        public static void UpdateBodyTemperaturePostfix(EntityBehaviorBodyTemperature __instance)
        {
            var entityAgent = EntityAgentField.GetValue(__instance) as Vintagestory.API.Common.EntityAgent;
            if (entityAgent == null)
            {
                return;
            }

            float freezeResistance = entityAgent.Stats.GetBlended("freezeResistance");
            if (freezeResistance <= 0)
            {
                return;
            }

            if (NormalBodyTemperatureField.GetValue(__instance) is not float normalBodyTemperature)
            {
                return;
            }

            float bodyTemperatureDrop = normalBodyTemperature - __instance.CurBodyTemperature;
            if (bodyTemperatureDrop <= BaseDamagingTemperatureDrop + freezeResistance)
            {
                DamagingFreezeHoursField.SetValue(__instance, 0f);
            }
        }
    }
}
