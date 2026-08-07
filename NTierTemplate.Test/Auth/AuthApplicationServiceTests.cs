using Microsoft.Extensions.Options;
using Moq;
using NTierTemplate.Application;
using NTierTemplate.Application.Auth;
using NTierTemplate.Application.Email;
using NTierTemplate.Application.Queue;
using NTierTemplate.Application.Users;
using NTierTemplate.Users;

namespace NTierTemplate.Test.Auth;

[TestFixture]
public class AuthApplicationServiceTests
{
    private Mock<IUserApplicationService> userApplicationService = null!;
    private Mock<IQueueApplicationService> queueApplicationService = null!;
    private Mock<IEmailClient> emailClient = null!;
    private AuthApplicationService service = null!;

    [SetUp]
    public void SetUp()
    {
        this.userApplicationService = new Mock<IUserApplicationService>();
        this.queueApplicationService = new Mock<IQueueApplicationService>();
        this.emailClient = new Mock<IEmailClient>();
        this.service = new AuthApplicationService(
            this.userApplicationService.Object,
            this.queueApplicationService.Object,
            this.emailClient.Object,
            Options.Create(new AppOptions { FrontendBaseUrl = "http://localhost:8240" })
        );
    }

    [Test]
    public async Task EnqueueRegisterUserAsync_EnqueuesRegisterUserCommand()
    {
        RegisterUserCommand? capturedCommand = null;

        this.queueApplicationService
            .Setup(queue => queue.EnqueueAsync(It.IsAny<RegisterUserCommand>(), It.IsAny<CancellationToken>()))
            .Callback<RegisterUserCommand, CancellationToken>((command, _) => capturedCommand = command)
            .ReturnsAsync(Guid.NewGuid());

        var messageId = await this.service.EnqueueRegisterUserAsync(
            new RegisterUserRequest
            {
                Email = "new@example.com",
                Password = "Password1",
                FirstName = "Ada",
                LastName = "Lovelace",
            }
        );

        messageId.Should().NotBeEmpty();
        capturedCommand.Should().NotBeNull();
        capturedCommand!.Email.Should().Be("new@example.com");

        this.queueApplicationService.Verify(
            queue => queue.EnqueueAsync(It.IsAny<RegisterUserCommand>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Test]
    public async Task ProcessRegisterUserAsync_ReturnsDuplicateFailure_WhenEmailAlreadyExists()
    {
        this.userApplicationService
            .Setup(service => service.RegisterAsync(It.IsAny<RegisterUserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserCreateResult
            {
                Succeeded = false,
                Errors = ["An account with that email already exists."],
            });

        var result = await this.service.ProcessRegisterUserAsync(
            new RegisterUserRequest
            {
                Email = "existing@example.com",
                Password = "Password1",
            }
        );

        result.Succeeded.Should().BeFalse();
        result.IsDuplicateEmail.Should().BeTrue();
        this.emailClient.Verify(
            client => client.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }
}
