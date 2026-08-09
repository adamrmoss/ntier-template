using NTierTemplate.Data.FailedCommands;
using NTierTemplate.Test.Support;

namespace NTierTemplate.Test.NTierTemplate.Data.FailedCommands;

[TestFixture]
public class FailedCommandDaoTests : InMemoryDataTestBase
{
    private FailedCommandDao failedCommandDao = null!;

    [SetUp]
    public void SetUpFailedCommandDao()
    {
        this.failedCommandDao = new FailedCommandDao(this.DbContext);
    }

    [Test]
    public async Task UpsertAsync_InsertsThenUpdatesExistingRow()
    {
        var messageId = Guid.NewGuid();

        await this.failedCommandDao.UpsertAsync(
            new FailedCommand
            {
                MessageId = messageId,
                CommandName = typeof(global::NTierTemplate.Users.RegisterUserCommand).FullName!,
                Payload = "{}",
                AttemptCount = 1,
                MaxAttempts = 3,
                LastError = "first failure",
                Status = FailedCommandStatus.PendingRetry,
                FailedAtUtc = DateTime.UtcNow,
                NextRetryAtUtc = DateTime.UtcNow.AddMinutes(1),
            }
        );

        await this.failedCommandDao.UpsertAsync(
            new FailedCommand
            {
                MessageId = messageId,
                CommandName = typeof(global::NTierTemplate.Users.RegisterUserCommand).FullName!,
                Payload = "{}",
                AttemptCount = 2,
                MaxAttempts = 3,
                LastError = "second failure",
                Status = FailedCommandStatus.PendingRetry,
                FailedAtUtc = DateTime.UtcNow,
                NextRetryAtUtc = DateTime.UtcNow.AddMinutes(2),
            }
        );

        var dueCommands = await this.failedCommandDao.GetDueForRetryAsync(
            DateTime.UtcNow.AddMinutes(3),
            take: 10
        );

        dueCommands.Should().ContainSingle(command => command.MessageId == messageId);
        dueCommands[0].AttemptCount.Should().Be(2);
        dueCommands[0].LastError.Should().Be("second failure");
    }

    [Test]
    public async Task GetDueForRetryAsync_ReturnsOnlyPendingRetryCommandsPastNextRetryTime()
    {
        var dueMessageId = Guid.NewGuid();
        var futureMessageId = Guid.NewGuid();

        await this.failedCommandDao.UpsertAsync(
            new FailedCommand
            {
                MessageId = dueMessageId,
                CommandName = "Test.Command",
                Payload = "{}",
                AttemptCount = 1,
                MaxAttempts = 3,
                Status = FailedCommandStatus.PendingRetry,
                FailedAtUtc = DateTime.UtcNow.AddMinutes(-5),
                NextRetryAtUtc = DateTime.UtcNow.AddMinutes(-1),
            }
        );
        await this.failedCommandDao.UpsertAsync(
            new FailedCommand
            {
                MessageId = futureMessageId,
                CommandName = "Test.Command",
                Payload = "{}",
                AttemptCount = 1,
                MaxAttempts = 3,
                Status = FailedCommandStatus.PendingRetry,
                FailedAtUtc = DateTime.UtcNow,
                NextRetryAtUtc = DateTime.UtcNow.AddHours(1),
            }
        );

        var dueCommands = await this.failedCommandDao.GetDueForRetryAsync(DateTime.UtcNow, take: 10);

        dueCommands.Should().ContainSingle(command => command.MessageId == dueMessageId);
    }

    [Test]
    public async Task MarkSucceededAsync_UpdatesStatusAndClearsNextRetry()
    {
        var messageId = Guid.NewGuid();

        await this.failedCommandDao.UpsertAsync(
            new FailedCommand
            {
                MessageId = messageId,
                CommandName = "Test.Command",
                Payload = "{}",
                AttemptCount = 1,
                MaxAttempts = 3,
                Status = FailedCommandStatus.PendingRetry,
                FailedAtUtc = DateTime.UtcNow,
                NextRetryAtUtc = DateTime.UtcNow,
            }
        );

        await this.failedCommandDao.MarkSucceededAsync(messageId);

        var dueCommands = await this.failedCommandDao.GetDueForRetryAsync(DateTime.UtcNow.AddHours(1), take: 10);

        dueCommands.Should().NotContain(command => command.MessageId == messageId);
    }
}
