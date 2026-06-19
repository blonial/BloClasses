using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace BloClasses.Extensions
{
    public static class TraitRequirementUtil
    {
        public static bool PlayerHasTrait(IPlayer? player, string? traitCode)
        {
            if (player == null || string.IsNullOrEmpty(traitCode))
            {
                return true;
            }

            var charSystem = player.Entity.World.Api.ModLoader.GetModSystem<CharacterSystem>();
            if (charSystem == null)
            {
                return false;
            }

            string characterClassCode = player.Entity.WatchedAttributes.GetString("characterClass");
            var charClass = charSystem.characterClasses.Find(c => c.Code == characterClassCode);
            return charClass?.Traits.Contains(traitCode) == true;
        }
    }
}
