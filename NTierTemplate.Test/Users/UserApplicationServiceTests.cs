using Microsoft.Extensions.Options;
using Moq;
using NTierTemplate.Application;
using NTierTemplate.Application.Email;
using NTierTemplate.Application.Queue;
using NTierTemplate.Application.Users;
using NTierTemplate.Data.Users;
using NTierTemplate.Users;

namespace NTierTemplate.Test.Users;

[TestFixture]
public class UserApplicationServiceTests
{
    private Mock<IUserDao> userDao = null!;
    private Mock<IQueueApplicationService> queueApplicationService = null!;
    private Mock<IEmailClient> emailClient = null!;
    private UserApplicationService service = null!;

    [SetUp]
    public void SetUp()
    {
        this.userDao = new Mock<IUserDao>();
        this.queueApplicationService = new Mock<IQueueApplicationService>();
        this.emailClient = new Mock<IEmailClient>();
        this.service = new UserApplicationService(
            this.userDao.Object,
            this.queueApplicationService.Object,
            this.emailClient.Object,
            Options.Create(new AppOptions { FrontendBaseUrl = "http://localhost:8240" })
        );
    }

    [Test]
    public async Task RegisterAsync_ReturnsFailure_WhenEmailAlreadyExists()
    {
        this.userDao
            .Setup(dao => dao.GetByEmailAsync("existing@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(this.CreateUser(email: "existing@example.com"));

        var result = await this.service.RegisterAsync(
            new RegisterUserRequest
            {
                Email = "existing@example.com",
                Password = "password",
            }
        );

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("An account with that email already exists.");
        this.userDao.Verify(
            dao => dao.CreateAsync(It.IsAny<RegisterUserRequest>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Test]
    public async Task RegisterAsync_NormalizesRequestBeforeCreatingUser()
    {
        RegisterUserRequest? capturedRequest = null;

        this.userDao
            .Setup(dao => dao.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        this.userDao
            .Setup(dao => dao.CreateAsync(It.IsAny<RegisterUserRequest>(), It.IsAny<CancellationToken>()))
            .Callback<RegisterUserRequest, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(new UserCreateResult
            {
                Succeeded = true,
                User = this.CreateUser(email: "new@example.com"),
            });

        await this.service.RegisterAsync(
            new RegisterUserRequest
            {
                Email = "  new@example.com  ",
                Password = "password",
                DisplayName = "  Ada  ",
                FirstName = "  Ada  ",
                LastName = "  Lovelace  ",
            }
        );

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Email.Should().Be("new@example.com");
        capturedRequest.DisplayName.Should().Be("Ada");
        capturedRequest.FirstName.Should().Be("Ada");
        capturedRequest.LastName.Should().Be("Lovelace");
    }

    [Test]
    public async Task CreateAdminAsync_ReturnsFailure_WhenEmailAlreadyExists()
    {
        this.userDao
            .Setup(dao => dao.GetByEmailAsync("admin@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(this.CreateUser(email: "admin@example.com"));

        var result = await this.service.CreateAdminAsync("admin@example.com", "password");

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("A user with that email already exists.");
        this.userDao.Verify(
            dao => dao.CreateAdminAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
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
    }

    [Test]
    public async Task ProcessRegisterUserAsync_ReturnsDuplicateFailure_WhenEmailAlreadyExists()
    {
        this.userDao
            .Setup(dao => dao.GetByEmailAsync("existing@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(this.CreateUser(email: "existing@example.com"));

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

    private User CreateUser(string email = "user@example.com")
    {
        return new User
        {
            Id = 1,
            Email = email,
            FirstName = "Test",
            LastName = "User",
            DisplayName = "Test User",
            Roles = ["User"],
        };
    }
}
