using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace BloClasses.EntityBehaviors
{
    public class WalkingStickHeldBonusEntityBehavior(Entity entity) : EntityBehavior(entity)
    {
        private const string HungerRateStat = "hungerrate";
        private const string StatCode = "bcWalkingStickHeldBonus";
        private const float HungerRateModifier = -0.1f;

        private float timeSinceLastUpdate;

        public override string PropertyName()
        {
            return "bcWalkingStickHeldBonus";
        }

        public override void OnGameTick(float deltaTime)
        {
            if (entity is not EntityPlayer player)
            {
                return;
            }

            if (entity.World.PlayerByUid(player.PlayerUID) is IServerPlayer serverPlayer && serverPlayer.ConnectionState != EnumClientState.Playing)
            {
                return;
            }

            timeSinceLastUpdate += deltaTime;
            if (timeSinceLastUpdate < 0.25f)
            {
                return;
            }

            timeSinceLastUpdate = 0;
            if (WalkingStickHeldBonusUtil.PlayerHoldsCowWalkingStick(player))
            {
                entity.Stats.Set(HungerRateStat, StatCode, HungerRateModifier, false);
                return;
            }

            entity.Stats.Remove(HungerRateStat, StatCode);
        }
    }
}
