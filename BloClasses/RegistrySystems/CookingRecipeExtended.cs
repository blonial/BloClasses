using System.IO;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace BloClasses.RegistrySystems
{
    [DocumentAsJson]
    public class CookingRecipeExtended : CookingRecipe
    {
        [DocumentAsJson("Optional", "", false)]
        public string? RequiresTrait;

        public new void FromBytes(BinaryReader reader, IWorldAccessor resolver)
        {
            base.FromBytes(reader, resolver);
            RequiresTrait = reader.ReadString();
        }

        public new void ToBytes(BinaryWriter writer)
        {
            base.ToBytes(writer);
            if (RequiresTrait != null)
            {
                writer.Write(RequiresTrait);
            }
        }
    }
}
