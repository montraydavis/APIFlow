using APIFlow.Tests.Contexts;
using NUnit.Framework;

namespace APIFlow.Tests
{
    public class IntegrationTests
    {
        private APIFlowContext _context;

        [SetUp]
        public void Setup()
        {

        }

        [Test]
        public void Integration_WalkChain()
        {
            this._context.Execute<UserContext>()
                .Execute<UserInformationContext>((userInformationContext, inputModel) =>
                {
                    userInformationContext.ConfigureModel<UserContext>((u, o) => o.Id = u.Value?[0].Id ?? 0);
                });
        }

        public IntegrationTests()
        {
            this._context = new APIFlowContext();
        }
    }
}
