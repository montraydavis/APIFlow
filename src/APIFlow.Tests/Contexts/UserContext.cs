using APIFlow.Endpoint;
using APIFlow.Models;
using APIFlow.Tests.Fixtures;
using System.Web.Http;

namespace APIFlow.Tests.Contexts
{
    public class UserContext : ApiContext<List<User>>
    {
        [HttpGet()]
        [Route("https://aef3c493-6ff3-47a2-be7f-150688405f7e.mock.pstmn.io/Users")]
        public override void ApplyContext(EndpointInputModel model)
        {

        }

        public override void ConfigureClient(ref HttpClientWrapper wrapper)
        {
            base.ConfigureClient(ref wrapper);

            // Add necessary headers etc.
        }

        public UserContext(List<User> baseObject,
            EndpointInputModel inputModel) : base(baseObject, inputModel)
        {

        }
    }
}
