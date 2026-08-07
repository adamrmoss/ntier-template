using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NTierTemplate.Application.Queue;
using NTierTemplate.Data.FailedCommands;
using NTierTemplate.Messaging;
using NTierTemplate.Users;

namespace NTierTemplate.Test.Queue;

[TestFixture]
public class QueueApplicationServiceTests
{
    private Mock<IFailedCommandDao> failedCommandDao = null!;
    private Mock<IServiceScopeFactory> scopeFactory = null!;
    private Mock<IServiceScope> scope = null!;
    private Mock<IServiceProvider> serviceProvider = null!;
    private QueueApplicationService service = null!;

    [SetUp]
    public void SetUp()
    {
        this.failedCommandDao = new Mock<IFailedCommandDao>();
        this.scopeFactory = new Mock<IServiceScopeFactory>();
        this.scope = new Mock<IServiceScope>();
        this.serviceProvider = new Mock<IServiceProvider>();

        this.scope.Setup(s => s.ServiceProvider).Returns(this.serviceProvider.Object);
        this.scopeFactory.Setup(f => f.CreateScope()).Returns(this.scope.Object);
        this.serviceProvider
            .Setup(p => p.GetService(typeof(IFailedCommandDao)))
            .Returns(this.failedCommandDao.Object);
        this.serviceProvider
            .Setup(p => p.GetService(typeof(IEnumerable<ICommandHandler>)))
            .Returns(Array.Empty<ICommandHandler>());

        this.service = new QueueApplicationService(
            this.scopeFactory.Object,
            Options.Create(new RabbitMqOptions()),
            NullLogger<QueueApplicationService>.Instance
        );
    }

    [TearDown]
    public async Task TearDown()
    {
        await this.service.DisposeAsync();
    }

    [Test]
    public async Task ProcessAsync_StoresExhausted_WhenCommandTypeIsUnknown()
    {
        FailedCommand? capturedCommand = null;

        this.failedCommandDao
            .Setup(dao => dao.UpsertAsync(It.IsAny<FailedCommand>(), It.IsAny<CancellationToken>()))
            .Callback<FailedCommand, CancellationToken>((command, _) => capturedCommand = command)
            .Returns(Task.CompletedTask);

        await this.service.ProcessAsync(
            new CommandEnvelope
            {
                MessageId = Guid.NewGuid(),
                CommandName = "Unknown.Command.Type",
                Payload = "{}",
                Attempt = 1,
                MaxAttempts = 3,
            }
        );

        capturedCommand.Should().NotBeNull();
        capturedCommand!.Status.Should().Be(FailedCommandStatus.Exhausted);
        capturedCommand.NextRetryAtUtc.Should().BeNull();
    }

    [Test]
    public async Task ProcessAsync_StoresExhausted_WhenRegisterUserPayloadIsInvalid()
    {
        FailedCommand? capturedCommand = null;

        this.failedCommandDao
            .Setup(dao => dao.UpsertAsync(It.IsAny<FailedCommand>(), It.IsAny<CancellationToken>()))
            .Callback<FailedCommand, CancellationToken>((command, _) => capturedCommand = command)
            .Returns(Task.CompletedTask);

        await this.service.ProcessAsync(
            new CommandEnvelope
            {
                MessageId = Guid.NewGuid(),
                CommandName = typeof(RegisterUserCommand).FullName!,
                Payload = "not-json",
                Attempt = 1,
                MaxAttempts = 3,
            }
        );

        capturedCommand.Should().NotBeNull();
        capturedCommand!.Status.Should().Be(FailedCommandStatus.Exhausted);
    }
}
