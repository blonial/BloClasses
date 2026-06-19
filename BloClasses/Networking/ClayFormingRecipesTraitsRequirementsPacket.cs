using ProtoBuf;

namespace BloClasses.Networking
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class ClayFormingRecipesTraitsRequirementsPacket
    {
        public string? Data;
    }
}
