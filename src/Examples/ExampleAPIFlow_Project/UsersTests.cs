using APIFlow;
using APIFlow.Models;

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
            var users = context.Walk<UserContext>();

            Assert.That(users.Response, Is.Not.Null);

            // Resolve value
            var usersList = context.GetValue<UserContext, User>(users.Response);

            // Assert Database and API items match.
            Assert.IsTrue(fakeDbItems.All(x => users.Response != null && users.Response.Any(y =>
            {
                if(y is UserContext ctx)
                {

                }

                return true;
            })));
        }

        public UserTests()
        {
            this.context = new APIFlowContext();
        }
    }
}