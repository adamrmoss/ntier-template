using NTierTemplate.Users;

namespace NTierTemplate.Test.NTierTemplate.Users;

[TestFixture]
public class RegisterUserCommandTests
{
    [Test]
    public void ToRequest_MapsAllFields()
    {
        var command = new RegisterUserCommand
        {
            Email = "ada@example.com",
            Password = "Password1",
            DisplayName = "Ada",
            FirstName = "Ada",
            LastName = "Lovelace",
        };

        var request = command.ToRequest();

        request.Email.Should().Be("ada@example.com");
        request.Password.Should().Be("Password1");
        request.DisplayName.Should().Be("Ada");
        request.FirstName.Should().Be("Ada");
        request.LastName.Should().Be("Lovelace");
    }
}
