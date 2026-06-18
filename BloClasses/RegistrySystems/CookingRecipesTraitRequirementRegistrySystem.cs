using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BloClasses.Networking;
using BloClasses.RegistrySystems;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace BloClasses.RecipeRegistrySystems
{
    public class CookingRecipesTraitRequirementRegistrySystem : ModSystem
    {
        public List<CookingRecipeRequiresTrait> CookingRecipesTraitRequirements = new List<CookingRecipeRequiresTrait>();

        private IServerNetworkChannel? ServerChannel;

        public override double ExecuteOrder()
        {
            return 0.6;
        }

        public override void Start(ICoreAPI api)
        {
            if (api.Side == EnumAppSide.Client && api is ICoreClientAPI capi)
            {
                capi.Network
                    .RegisterChannel("cookingrecipestraitrequirement")
                    .RegisterMessageType<CookingRecipesTraitsRequirementsPacket>()
                    .SetMessageHandler<CookingRecipesTraitsRequirementsPacket>(OnPacketReceived);
            }
            else if (api.Side == EnumAppSide.Server && api is ICoreServerAPI sapi)
            {
                ServerChannel = sapi.Network
                    .RegisterChannel("cookingrecipestraitrequirement")
                    .RegisterMessageType<CookingRecipesTraitsRequirementsPacket>();
                sapi.Event.PlayerJoin += SendToClient;
            }
        }

        public override void AssetsLoaded(ICoreAPI api)
        {
            if (!(api is ICoreServerAPI coreServerAPI))
            {
                return;
            }

            Dictionary<AssetLocation, JToken> many = coreServerAPI.Assets.GetMany<JToken>(coreServerAPI.Server.Logger, "recipes/cooking");
            foreach (KeyValuePair<AssetLocation, JToken> item in many)
            {
                if (item.Value is JObject)
                {
                    LoadRecipe(coreServerAPI, item.Key, item.Value);
                }

                if (!(item.Value is JArray))
                {
                    continue;
                }

                foreach (JToken item2 in (JArray)item.Value)
                {
                    LoadRecipe(coreServerAPI, item.Key, item2);
                }
            }
        }

        private void LoadRecipe(ICoreServerAPI sapi, AssetLocation loc, JToken jrec)
        {
            CookingRecipeExtended? cookingRecipe = jrec.ToObject<CookingRecipeExtended>(loc.Domain);
            if (cookingRecipe != null && cookingRecipe.Enabled)
            {
                CookingRecipesTraitRequirements.Add(new CookingRecipeRequiresTrait
                {
                    Code = cookingRecipe.Code,
                    RequiresTrait = cookingRecipe.RequiresTrait
                });
            }
        }

        private void SendToClient(IServerPlayer player)
        {
            var packet = new CookingRecipesTraitsRequirementsPacket
            {
                Data = JsonConvert.SerializeObject(CookingRecipesTraitRequirements)
            };

            ServerChannel?.SendPacket(packet, player);
        }

        private void OnPacketReceived(CookingRecipesTraitsRequirementsPacket packet)
        {
            if (packet == null || packet.Data == null)
            {
                CookingRecipesTraitRequirements = new List<CookingRecipeRequiresTrait>() { };
                return;
            }

            var data = JsonConvert.DeserializeObject<List<CookingRecipeRequiresTrait>>(packet.Data);
            if (data != null)
            {
                CookingRecipesTraitRequirements = data;
            }
            else
            {
                CookingRecipesTraitRequirements = new List<CookingRecipeRequiresTrait>() { };
            }
        }
    }
}
