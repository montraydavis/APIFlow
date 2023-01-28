using APIFlow.Endpoint;
using APIFlow.Models;
using APIFlow.Tests.Fixtures;
using System.Web.Http;

namespace APIFlow.Tests.Contexts
{
    public class UserInformationContext_BadConfiguration : ApiContext<UserInformation>
    {

        public override void ConfigureEndpoint(ref string endpoint, EndpointInputModel inputModel, bool randomizedInput = false)
        {
            base.ConfigureEndpoint(ref endpoint, inputModel, randomizedInput);
            base.ConfigureEndpoint<UserContext>("Id", (u) => u.Value?[0].Id ?? 0);
        }

        /// <summary>
        /// Configure inputModel which are forwarded to the next endpoint(s).
        /// </summary>
        /// <param name="inputModel">Input Model.</param>
        [HttpGet()]
        public override void ApplyContext(EndpointInputModel inputModel)
        {
            base.ConfigureModel<UserContext>((u, o) => o.Id = u.Value?[0].Id ?? 0);
            base.ConfigureEndpoint<UserContext>("Id", (u) => u.Value?[0].Id ?? 0);
        }

        public UserInformationContext_BadConfiguration(UserInformation baseObject, EndpointInputModel inputModel) : base(baseObject, inputModel)
        {

        }
    }
}