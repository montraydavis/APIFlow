using APIFlow;

namespace ExampleAPIFlow_Project
{
    public class UserTests
    {
        APIFlowContext context;

        [SetUp]
        public void Setup()
        {

        }

        private User[] GetUsersFromDatabase()
        {
            return new[]
            {
                new User(){ Id = 1 },
                new User(){ Id = 2 }
            };
        }

        [Test]
        public void Test1()
        {
            // Fetch records from database
            var fakeDbItems = GetUsersFromDatabase();

            // Execute /users endpoint
            var users = context.Walk<UserContext>().Response;
            var userInfo = context.Walk<UserInformationContext>().Response;

            Assert.That(users, Is.Not.Null);
            Assert.That(userInfo, Is.Not.Null);

            // Resolve value
            var usersList = context.GetValue<UserContext, List<User>>(users);
            var userInformation = context.GetValue<UserInformationContext, UserInformation>(userInfo);

            // Assert /users?id={User.Id}
            Assert.That(usersList.Any(x => x.Id == userInformation.Id), Is.True);

            // Assert Database and API items match.
            Assert.IsTrue(fakeDbItems.All(x => users != null && users.Any(y =>
            {
                if (y is UserContext ctx)
                {
                    return true;
                }

                return false;
            })));
        }

        public UserTests()
        {
            this.context = new APIFlowContext();
        }
    }
}