using ProtoBuf;

namespace RPClasses.Networking
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class CookingRecipesTraitsRequirementsPacket
    {
        public string? Data;
    }
}
