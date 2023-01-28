using APIFlow;
using APIFlow.Endpoint;
using APIFlow.Repositories;
using ExampleAPIFlow_Project.Fixtures;
using System.Web.Http;

namespace ExampleAPIFlow_Project.Contexts
{
    public class UserContext : ApiContext<List<User>, HTTPDataExtender>
    {
        public override bool HasBody => false;

        [HttpGet()]
        [Route("https://aef3c493-6ff3-47a2-be7f-150688405f7e.mock.pstmn.io/Users")]
        public override void ApplyContext(APIFlowInputModel model)
        {

        }

        public override void ConfigureClient(ref HttpClientWrapper wrapper)
        {
            base.ConfigureClient(ref wrapper);

            // Add necessary headers etc.
        }

        public UserContext(List<User> baseObject,
            APIFlowInputModel inputModel) : base(baseObject, inputModel)
        {

        }
    }
}
