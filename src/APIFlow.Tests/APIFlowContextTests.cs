using APIFlow.Tests.Contexts;
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
        [Theory]
        public void APIFlowContext_GetHttpVerbMethod(object httpVerbAttribute)
        {
            Assert.That(httpVerbAttribute as Attribute, Is.Not.Null);

            var verb = this.context.GetHttpVerbMethod(httpVerbAttribute as Attribute);

            Assert.That(httpVerbAttribute.GetType().FullName?.ToUpper(), Does.Contain(verb.Method.ToUpper()));
        }

        [Test]
        public void APIFlowContext_End2End()
        {
            this.context.Walk<UserContext>()
                .Walk<UserInformationContext>();
        }
    }
}
