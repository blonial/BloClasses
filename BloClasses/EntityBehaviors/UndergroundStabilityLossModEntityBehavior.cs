using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace BloClasses.EntityBehaviors
{
    public class UndergroundStabilityLossModEntityBehavior(Entity entity) : EntityBehavior(entity)
    {
        private float timeSinceLastUpdate = 0.0f;

        private readonly int SunLightLevelForInCave = 5;

        private EntityBehaviorTemporalStabilityAffected? TemporalAffected => entity.GetBehavior<EntityBehaviorTemporalStabilityAffected>();

        public override string PropertyName()
        {
            return "bcUndergroundStabilityLossMod";
        }

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);
        }

        public override void OnGameTick(float deltaTime)
        {
            if (entity == null || entity is not EntityPlayer player || TemporalAffected == null)
            {
                return;
            }

            if (entity.World.PlayerByUid(player.PlayerUID) is IServerPlayer serverPlayer && serverPlayer.ConnectionState != EnumClientState.Playing)
            {
                return;
            }

            timeSinceLastUpdate += deltaTime;

            if (timeSinceLastUpdate > 1.0f)
            {
                timeSinceLastUpdate = 0.0f;

                HandleStabilityLossModifiers();
            }
        }

        private void HandleStabilityLossModifiers()
        {
            if (TemporalAffected == null)
            {
                return;
            }

            var tempStabVelocity = TemporalAffected.TempStabChangeVelocity;
            if (tempStabVelocity >= 0)
            {
                return;
            }

            var modifiedVelocity = tempStabVelocity * entity.Stats.GetBlended("overallStabilityLossMod");
            if (entity.World.BlockAccessor.GetLightLevel(entity.Pos.AsBlockPos, EnumLightLevelType.OnlySunLight) < SunLightLevelForInCave)
            {
                modifiedVelocity *= entity.Stats.GetBlended("undergroundStabilityLossMod");
            }

            TemporalAffected.TempStabChangeVelocity = modifiedVelocity;
        }
    }
}
