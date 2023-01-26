using System.Text.Json.Serialization;

namespace APIFlow.Tests.Fixtures
{
    // Insert Update User
    public class UserInformation
    {
        [JsonIgnore]
        public int Id { get; set; }
        public string Email { get; set; }

        public UserInformation()
        {
            this.Email = string.Empty;
        }
    }
}
