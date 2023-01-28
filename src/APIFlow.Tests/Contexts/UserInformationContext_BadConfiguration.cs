using APIFlow.Endpoint;
using APIFlow.Repositories;
using APIFlow.Tests.Fixtures;
using System.Web.Http;

namespace APIFlow.Tests.Contexts
{
    public class UserInformationContext_BadConfiguration : ApiContext<UserInformation, HTTPDataExtender>
    {

        public override void ConfigureEndpoint(ref string endpoint, APIFlowInputModel inputModel, bool randomizedInput = false)
        {
            base.ConfigureEndpoint(ref endpoint, inputModel, randomizedInput);
            base.ConfigureEndpoint<UserContext>("Id", (u) => u.Value?[0].Id ?? 0);
        }

        /// <summary>
        /// Configure inputModel which are forwarded to the next endpoint(s).
        /// </summary>
        /// <param name="inputModel">Input Model.</param>
        [HttpGet()]
        public override void ApplyContext(APIFlowInputModel inputModel)
        {
            base.ConfigureModel<UserContext>((u, o) => o.Id = u.Value?[0].Id ?? 0);
            base.ConfigureEndpoint<UserContext>("Id", (u) => u.Value?[0].Id ?? 0);
        }

        public UserInformationContext_BadConfiguration(UserInformation baseObject, APIFlowInputModel inputModel) : base(baseObject, inputModel)
        {

        }
    }
}