using APIFlow.Endpoint;
using APIFlow.Models;
using System.Web.Http;

namespace ExampleAPIFlow_Project.Contexts
{
    public class UserInformationContext : ApiContext<UserInformation>
    {
        public override void ConfigureEndpoint(ref string endpoint, EndpointInputModel inputModel, bool randomizedInput = false)
        {
            base.ConfigureEndpoint(ref endpoint, inputModel, randomizedInput);

            ConfigureEndpoint<UserContext>("Id", (u) => u.Value[0].Id);
        }
        /// <summary>
        /// Configure inputModel which are forwarded to the next endpoint(s).
        /// </summary>
        /// <param name="inputModel">Input Model.</param>
        [HttpGet()]
        [Route("https://aef3c493-6ff3-47a2-be7f-150688405f7e.mock.pstmn.io/UserInformation")]
        public override void ApplyContext(EndpointInputModel inputModel)
        {
            ConfigureModel<UserContext>((u, o) => o.Id = u.Value[0].Id);
            ConfigureEndpoint<UserContext>("Id", (u) => u.Value[0].Id);
        }

        public UserInformationContext(UserInformation baseObject, EndpointInputModel inputModel) : base(baseObject, inputModel)
        {

        }
    }
}