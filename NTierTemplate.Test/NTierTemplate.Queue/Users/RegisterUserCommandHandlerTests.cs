using Moq;
using NTierTemplate.Application.Queue;
using NTierTemplate.Application.Users;
using NTierTemplate.Queue.Users;
using NTierTemplate.Users;

namespace NTierTemplate.Test.NTierTemplate.Queue.Users;

[TestFixture]
public class RegisterUserCommandHandlerTests
{
    private Mock<IUserApplicationService> userApplicationService = null!;
    private RegisterUserCommandHandler handler = null!;

    [SetUp]
    public void SetUp()
    {
        this.userApplicationService = new Mock<IUserApplicationService>();
        this.handler = new RegisterUserCommandHandler(this.userApplicationService.Object);
    }

    [Test]
    public async Task HandleAsync_ReturnsSuccess_WhenRegistrationCompletes()
    {
        this.userApplicationService
            .Setup(service => service.ProcessRegisterUserAsync(It.IsAny<RegisterUserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessRegisterUserResult
            {
                Succeeded = true,
            });

        ICommandHandler handler = this.handler;

        var result = await handler.HandleAsync(
            new RegisterUserCommand
            {
                Email = "new@example.com",
                Password = "Password1",
            }
        );

        result.Succeeded.Should().BeTrue();
    }

    [Test]
    public async Task HandleAsync_ReturnsPermanentFailure_WhenDuplicateEmailIsReported()
    {
        this.userApplicationService
            .Setup(service => service.ProcessRegisterUserAsync(It.IsAny<RegisterUserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessRegisterUserResult
            {
                Succeeded = false,
                IsDuplicateEmail = true,
                ErrorMessage = "Duplicate email.",
            });

        var result = await ((ICommandHandler)this.handler).HandleAsync(
            new RegisterUserCommand
            {
                Email = "existing@example.com",
                Password = "Password1",
            }
        );

        result.Succeeded.Should().BeFalse();
        result.IsPermanentFailure.Should().BeTrue();
    }

    [Test]
    public async Task HandleAsync_ReturnsTransientFailure_WhenRegistrationFailsTransiently()
    {
        this.userApplicationService
            .Setup(service => service.ProcessRegisterUserAsync(It.IsAny<RegisterUserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessRegisterUserResult
            {
                Succeeded = false,
                ErrorMessage = "SMTP unavailable.",
            });

        ICommandHandler handler = this.handler;

        var result = await handler.HandleAsync(
            new RegisterUserCommand
            {
                Email = "new@example.com",
                Password = "Password1",
            }
        );

        result.Succeeded.Should().BeFalse();
        result.IsPermanentFailure.Should().BeFalse();
    }
}
