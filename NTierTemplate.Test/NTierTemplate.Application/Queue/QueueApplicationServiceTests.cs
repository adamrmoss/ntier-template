using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NTierTemplate.Application.Ioc;
using NTierTemplate.Application.Queue;
using NTierTemplate.Data.FailedCommands;
using NTierTemplate.Messaging;
using NTierTemplate.Test.Support;
using NTierTemplate.Users;

namespace NTierTemplate.Test.NTierTemplate.Application.Queue;

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
            TestJsonOptionsMonitor.CreatePascalCaseMonitor(),
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

    [Test]
    public async Task ProcessAsync_MarksSucceeded_WhenHandlerSucceeds()
    {
        var messageId = Guid.NewGuid();
        var handler = new StubCommandHandler(typeof(RegisterUserCommand), CommandHandlerResult.Success());

        this.serviceProvider
            .Setup(p => p.GetService(typeof(IEnumerable<ICommandHandler>)))
            .Returns(new ICommandHandler[] { handler });

        await this.service.ProcessAsync(
            new CommandEnvelope
            {
                MessageId = messageId,
                CommandName = typeof(RegisterUserCommand).FullName!,
                Payload = JsonSerializer.Serialize(new RegisterUserCommand
                {
                    Email = "new@example.com",
                    Password = "Password1",
                }),
                Attempt = 1,
                MaxAttempts = 3,
            }
        );

        this.failedCommandDao.Verify(
            dao => dao.MarkSucceededAsync(messageId, It.IsAny<CancellationToken>()),
            Times.Once
        );
        this.failedCommandDao.Verify(
            dao => dao.UpsertAsync(It.IsAny<FailedCommand>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Test]
    public async Task ProcessAsync_SchedulesRetry_WhenHandlerReturnsTransientFailure()
    {
        FailedCommand? capturedCommand = null;
        var handler = new StubCommandHandler(
            typeof(RegisterUserCommand),
            CommandHandlerResult.TransientFailure("SMTP unavailable.")
        );

        this.serviceProvider
            .Setup(p => p.GetService(typeof(IEnumerable<ICommandHandler>)))
            .Returns(new ICommandHandler[] { handler });
        this.failedCommandDao
            .Setup(dao => dao.UpsertAsync(It.IsAny<FailedCommand>(), It.IsAny<CancellationToken>()))
            .Callback<FailedCommand, CancellationToken>((command, _) => capturedCommand = command)
            .Returns(Task.CompletedTask);

        await this.service.ProcessAsync(
            new CommandEnvelope
            {
                MessageId = Guid.NewGuid(),
                CommandName = typeof(RegisterUserCommand).FullName!,
                Payload = JsonSerializer.Serialize(new RegisterUserCommand
                {
                    Email = "new@example.com",
                    Password = "Password1",
                }),
                Attempt = 1,
                MaxAttempts = 3,
            }
        );

        capturedCommand.Should().NotBeNull();
        capturedCommand!.Status.Should().Be(FailedCommandStatus.PendingRetry);
        capturedCommand.NextRetryAtUtc.Should().NotBeNull();
    }

    private sealed class StubCommandHandler(Type commandType, CommandHandlerResult result) : ICommandHandler
    {
        public Type CommandType { get; } = commandType;

        public Task<CommandHandlerResult> HandleAsync(object command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(result);
        }
    }
}
