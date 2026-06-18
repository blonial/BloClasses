using System.Collections.Generic;
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

        private bool HasUndergroundStabilityLossMod = false;

        private readonly int SunLightLevelForInCave = 5;

        private List<string> AffectedClasses = new List<string>() { "bcfarmer" };

        private EntityBehaviorTemporalStabilityAffected? TemporalAffected => entity.GetBehavior<EntityBehaviorTemporalStabilityAffected>();

        public override string PropertyName()
        {
            return "bcUndergroundStabilityLossMod";
        }

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);

            CheckForUndergroundStabilityLoss();
            entity.WatchedAttributes.RegisterModifiedListener("characterClass", CheckForUndergroundStabilityLoss);
        }

        public void CheckForUndergroundStabilityLoss()
        {
            if (entity == null || entity is not EntityPlayer)
            {
                HasUndergroundStabilityLossMod = false;
                return;
            }

            var charClass = entity.WatchedAttributes.GetString("characterClass");
            if (charClass == null)
            {
                HasUndergroundStabilityLossMod = false;
                return;
            }

            if (!AffectedClasses.Contains(charClass))
            {
                HasUndergroundStabilityLossMod = false;
                return;
            }

            HasUndergroundStabilityLossMod = true;
        }

        public override void OnGameTick(float deltaTime)
        {
            if (entity == null || entity is not EntityPlayer || TemporalAffected == null)
            {
                return;
            }

            if (entity.World.PlayerByUid(((EntityPlayer)entity).PlayerUID) is IServerPlayer serverPlayer && serverPlayer.ConnectionState != EnumClientState.Playing)
            {
                return;
            }

            if (!HasUndergroundStabilityLossMod)
            {
                return;
            }

            timeSinceLastUpdate += deltaTime;

            if (timeSinceLastUpdate > 1.0f)
            {
                timeSinceLastUpdate = 0.0f;

                HandleUndergroundStabilityLoss();
            }
        }

        private void HandleUndergroundStabilityLoss()
        {
            if (TemporalAffected == null)
            {
                return;
            }

            var tempStabVelocity = TemporalAffected.TempStabChangeVelocity;
            if (entity.World.BlockAccessor.GetLightLevel(entity.Pos.AsBlockPos, EnumLightLevelType.OnlySunLight) < SunLightLevelForInCave && tempStabVelocity < 0)
            {
                var caveLoss = entity.Stats.GetBlended("undergroundStabilityLossMod");
                TemporalAffected.TempStabChangeVelocity = (tempStabVelocity * caveLoss);
                return;
            }
        }
    }
}
