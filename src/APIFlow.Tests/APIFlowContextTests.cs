using APIFlow.Tests.Contexts;
using NUnit.Framework;
using System.Web.Http;

namespace APIFlow.Tests
{
    public class APIFlowContextTests
    {
        [DatapointSource]
        public object[] httpVerbAttributes = new[] { new HttpGetAttribute(), new HttpPostAttribute(), new HttpPutAttribute(), new HttpPatchAttribute(), new HttpDeleteAttribute() as Attribute };
        private APIFlowContext context;

        [SetUp]
        public void Setup()
        {
            this.context = new APIFlowContext();
        }

        [Test]
        public void APIFlowContext_End2End()
        {
            this.context.Execute<UserContext>()
                .Execute<UserInformationContext>();
        }
    }
}
