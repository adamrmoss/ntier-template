using System.Security.Claims;
using NTierTemplate.Application.Users;
using NTierTemplate.Cli;
using NTierTemplate.Test.Support;
using NTierTemplate.Users;

namespace NTierTemplate.Test.NTierTemplate.Cli;

[TestFixture]
public class CliPrincipalContainerTests
{
    [Test]
    public void SignIn_StoresAuthenticatedPrincipalForTheScope()
    {
        var container = new CliPrincipalContainer();
        var user = TestUsers.Create(id: 5, email: "admin@example.com", roles: ["Admin"]);

        container.SignIn(user);

        container.Principal.Should().NotBeNull();
        container.Principal!.Identity!.IsAuthenticated.Should().BeTrue();
        container.Principal.FindFirst(ClaimTypes.NameIdentifier)!.Value.Should().Be("5");
    }
}
