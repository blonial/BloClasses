using BloClasses.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace BloClasses.RegistrySystems
{
    public class ClayFormingRecipesTraitRequirementRegistrySystem : ModSystem
    {
        public List<ClayFormingRecipeRequiresTrait> ClayFormingRecipesTraitRequirements = new List<ClayFormingRecipeRequiresTrait>();

        private IServerNetworkChannel? ServerChannel;

        public override void Start(ICoreAPI api)
        {
            if (api.Side == EnumAppSide.Client && api is Vintagestory.API.Client.ICoreClientAPI capi)
            {
                capi.Network
                    .RegisterChannel("clayformingrecipestraitrequirement")
                    .RegisterMessageType<ClayFormingRecipesTraitsRequirementsPacket>()
                    .SetMessageHandler<ClayFormingRecipesTraitsRequirementsPacket>(OnPacketReceived);
            }
            else if (api.Side == EnumAppSide.Server && api is ICoreServerAPI sapi)
            {
                ServerChannel = sapi.Network
                    .RegisterChannel("clayformingrecipestraitrequirement")
                    .RegisterMessageType<ClayFormingRecipesTraitsRequirementsPacket>();
                sapi.Event.PlayerJoin += SendToClient;
            }
        }

        public override void AssetsLoaded(ICoreAPI api)
        {
            ClayFormingRecipesTraitRequirements.Clear();

            Dictionary<AssetLocation, JToken> many = api.Assets.GetMany<JToken>(api.World.Logger, "recipes/clayforming");
            foreach (KeyValuePair<AssetLocation, JToken> item in many)
            {
                if (item.Value is JObject)
                {
                    LoadRecipe(item.Key, item.Value);
                }

                if (item.Value is not JArray recipes)
                {
                    continue;
                }

                foreach (JToken recipe in recipes)
                {
                    LoadRecipe(item.Key, recipe);
                }
            }
        }

        private void LoadRecipe(AssetLocation loc, JToken jrec)
        {
            ClayFormingRecipe? recipe = jrec.ToObject<ClayFormingRecipe>(loc.Domain);
            if (recipe != null && recipe.Enabled && !string.IsNullOrEmpty(recipe.RequiresTrait))
            {
                ClayFormingRecipesTraitRequirements.Add(new ClayFormingRecipeRequiresTrait
                {
                    OutputCode = recipe.Output?.Code?.ToString(),
                    RequiresTrait = recipe.RequiresTrait
                });
            }
        }

        private void SendToClient(IServerPlayer player)
        {
            var packet = new ClayFormingRecipesTraitsRequirementsPacket
            {
                Data = JsonConvert.SerializeObject(ClayFormingRecipesTraitRequirements)
            };

            ServerChannel?.SendPacket(packet, player);
        }

        private void OnPacketReceived(ClayFormingRecipesTraitsRequirementsPacket packet)
        {
            if (packet?.Data == null)
            {
                ClayFormingRecipesTraitRequirements = new List<ClayFormingRecipeRequiresTrait>();
                return;
            }

            var data = JsonConvert.DeserializeObject<List<ClayFormingRecipeRequiresTrait>>(packet.Data);
            ClayFormingRecipesTraitRequirements = data ?? new List<ClayFormingRecipeRequiresTrait>();
        }
    }
}
