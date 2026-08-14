using System.Security.Claims;
using NTierTemplate.Users;

namespace NTierTemplate.Test.NTierTemplate.Users;

[TestFixture]
public class UserPrincipalTests
{
    [Test]
    public void Create_BuildsAuthenticatedPrincipalWithUserClaimsAndRoles()
    {
        var user = new User
        {
            Id = 42,
            Email = "user@example.com",
            FirstName = "Ada",
            LastName = "Lovelace",
            DisplayName = "Ada Lovelace",
            Roles = ["Admin", "User"],
        };

        var principal = UserPrincipal.Create(user, "Bearer");

        principal.Identity!.IsAuthenticated.Should().BeTrue();
        principal.Identity!.AuthenticationType.Should().Be("Bearer");
        principal.FindFirst(ClaimTypes.NameIdentifier)!.Value.Should().Be("42");
        principal.FindFirst(ClaimTypes.Email)!.Value.Should().Be("user@example.com");
        principal.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Should()
            .Equal("Admin", "User");
    }
}
