using Microsoft.AspNetCore.Mvc;
using Moq;
using NTierTemplate.Api.Users;
using NTierTemplate.Application.Auth;
using NTierTemplate.Test.Support;

namespace NTierTemplate.Test.NTierTemplate.Api.Users;

[TestFixture]
public class UserControllerTests
{
    [Test]
    public async Task GetCurrent_ReturnsUnauthorized_WhenUserIsMissing()
    {
        var authApplicationService = new Mock<IAuthApplicationService>();
        authApplicationService
            .Setup(auth => auth.GetCurrentUserAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((global::NTierTemplate.Users.User?)null);

        var controller = new UserController(
            authApplicationService.Object,
            TestJsonOptionsMonitor.CreateCamelCaseMonitor()
        );

        var result = await controller.GetCurrent(CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Test]
    public async Task GetCurrent_ReturnsOkJson_WhenUserIsPresent()
    {
        var user = TestUsers.Create(email: "user@example.com");
        var authApplicationService = new Mock<IAuthApplicationService>();
        authApplicationService
            .Setup(auth => auth.GetCurrentUserAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var controller = new UserController(
            authApplicationService.Object,
            TestJsonOptionsMonitor.CreateCamelCaseMonitor()
        );

        var result = await controller.GetCurrent(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        ((OkObjectResult)result).Value.Should().BeOfType<string>();
        ((string)((OkObjectResult)result).Value!).Should().Contain("user@example.com");
    }
}
