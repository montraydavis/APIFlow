using APIFlow.Endpoint;
using APIFlow.FlowExceptions;
using APIFlow.Models;
using APIFlow.Tests.Contexts;
using APIFlow.Tests.Fixtures;
using Moq;

namespace APIFlow.Tests
{
    public sealed partial class ForwardFeedTests
    {

        private Mock<UserInformationContext>? _moqUserInformation;
        private Mock<UserInformationContext_BadConfiguration>? _moqUserInformationBadConfiguration;
        private Mock<UserInformationContext_MissingRoute>? _moqUserInformationMissingRoute;
        private Mock<UserInformationContext_MissingRouteParameter>? _moqUserInformationMissingRouteParameter;
        private EndpointInputModel _inputModel;
        private UserInformationContext? _tmpUserInformationContext;
        private UserInformationContext_BadConfiguration? _tmpUserInformationBadConfigurationContext;
        private UserInformationContext_MissingRoute? _tmpUserInformationMissingRouteContext;
        private UserInformationContext_MissingRouteParameter? _tmpUserInformationMissingRouteParameterContext;

        [DatapointSource]
        private static readonly User[] userList = new[] {
            new User() { Id = 767 },
            new User() { Id = 823 }
        };

        private void Setup(User? user = null)
        {
            if (user == null) user = new User();

            var typeName = typeof(UserContext).FullName!;
            var tmpUserList = new User[] { user }.ToList();

            this._inputModel.Add(typeName, new List<object>() { new UserContext(tmpUserList, this._inputModel) });
            this._tmpUserInformationContext = new UserInformationContext(new UserInformation(), this._inputModel);
            this._tmpUserInformationBadConfigurationContext = new UserInformationContext_BadConfiguration(new UserInformation(), this._inputModel);
            this._tmpUserInformationMissingRouteContext = new UserInformationContext_MissingRoute(new UserInformation(), this._inputModel);
            this._tmpUserInformationMissingRouteParameterContext = new UserInformationContext_MissingRouteParameter(new UserInformation(), this._inputModel);
            this._moqUserInformation = new Mock<UserInformationContext>(null, this._inputModel)
            {
                CallBase = true
            };
            this._moqUserInformationBadConfiguration = new Mock<UserInformationContext_BadConfiguration>(null, this._inputModel)
            {
                CallBase = true
            };
            this._moqUserInformationMissingRoute = new Mock<UserInformationContext_MissingRoute>(null, this._inputModel)
            {
                CallBase = true
            };
            this._moqUserInformationMissingRouteParameter = new Mock<UserInformationContext_MissingRouteParameter>(null, this._inputModel)
            {
                CallBase = true
            };
        }

        [SetUp]
        public void SetupEach()
        {
            this._inputModel = new EndpointInputModel();
        }



        [Test]
        [Theory]
        public void ForwardFeed_ConfigureUrl(User user)
        {
            this.Setup(user);

            Assert.That(this._moqUserInformation, Is.Not.Null);
            Assert.That(this._tmpUserInformationContext, Is.Not.Null);

            this._moqUserInformation.Object.ResolveEndpointUrl(this._tmpUserInformationContext);
            this._moqUserInformation.Object.ApplyContext(_inputModel);

            Assert.That($"https://aef3c493-6ff3-47a2-be7f-150688405f7e.mock.pstmn.io/UserInformation?Id={user.Id}", Is.EqualTo(this._moqUserInformation.Object.EndpointUrl));
        }

        [Test]
        [Theory]
        public void ForwardFeed_ConfigureModel(User user)
        {
            this.Setup(user);

            Assert.That(this._moqUserInformation, Is.Not.Null);
            Assert.That(this._tmpUserInformationContext, Is.Not.Null);

            this._moqUserInformation.Object.ResolveEndpointUrl(this._tmpUserInformationContext);
            this._moqUserInformation.Object.ApplyContext(_inputModel);

            Assert.That(this._moqUserInformation.Object.Value.Id, Is.EqualTo(user.Id));
        }

        #region Negative Test Case Association
        [Test]
        public void Fail_ForwardFeed_ConfigureModel_MissingInput()
        {
            this.Setup();

            Assert.That(this._moqUserInformationMissingRoute, Is.Not.Null);

            Assert.Throws<APIFlowModelException>(() =>
            {
                this._moqUserInformationMissingRoute.Object.ConfigureModel<UserInformationContext>((u, o) => u.Value.Id = o.Id);
            });
        }
        [Test]
        [Theory]
        public void Fail_ForwardFeed_ConfigureUrl_MissingRoute(User user)
        {
            this.Setup(user);

            Assert.That(this._moqUserInformationMissingRoute, Is.Not.Null);
            Assert.That(this._tmpUserInformationMissingRouteContext, Is.Not.Null);

            Assert.Throws<APIFlowRouteException>(() =>
            {
                this._moqUserInformationMissingRoute.Object.ResolveEndpointUrl(this._tmpUserInformationMissingRouteContext);
            });
        }

        [Test]
        public void Fail_ForwardFeed_ConfigureUrl_MissingRouteParameter()
        {
            this.Setup();

            Assert.That(this._moqUserInformationBadConfiguration, Is.Not.Null);
            Assert.That(this._tmpUserInformationBadConfigurationContext, Is.Not.Null);

            this._moqUserInformationBadConfiguration.Object.ResolveEndpointUrl(this._tmpUserInformationBadConfigurationContext);
            this._moqUserInformationBadConfiguration.Object.ApplyContext(_inputModel);
        }
        #endregion
    }
}