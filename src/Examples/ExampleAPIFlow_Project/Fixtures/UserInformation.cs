using System.Text.Json.Serialization;

namespace ExampleAPIFlow_Project.Fixtures
{
    // Insert Update User
    public class UserInformation
    {
        [JsonIgnore]
        public int Id { get; set; }
        public string Email { get; set; }

        public UserInformation()
        {
            Email = string.Empty;
        }
    }
}
