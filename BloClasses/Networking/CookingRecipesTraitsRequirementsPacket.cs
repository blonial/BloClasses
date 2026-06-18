using ProtoBuf;

namespace BloClasses.Networking
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class CookingRecipesTraitsRequirementsPacket
    {
        public string? Data;
    }
}
