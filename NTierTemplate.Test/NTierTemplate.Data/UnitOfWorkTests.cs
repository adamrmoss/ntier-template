using NTierTemplate.Data;
using NTierTemplate.Data.Users;
using NTierTemplate.Test.Support;
using NTierTemplate.Users;

namespace NTierTemplate.Test.NTierTemplate.Data;

[TestFixture]
public class UnitOfWorkTests : InMemoryDataTestBase
{
    [Test]
    public async Task CommitAsync_PersistsChangesWithinTransaction()
    {
        var unitOfWork = new UnitOfWork(this.DbContext);
        var userDao = new UserDao(this.DbContext, this.UserManager, this.RoleManager);

        await unitOfWork.BeginTransactionAsync();
        var createResult = await userDao.CreateAsync(
            new RegisterUserRequest
            {
                Email = "saved@example.com",
                Password = "Password1",
            }
        );
        createResult.Succeeded.Should().BeTrue();
        await unitOfWork.CommitAsync();

        var loadedUser = await userDao.GetByEmailAsync("saved@example.com");

        loadedUser.Should().NotBeNull();
        loadedUser!.Email.Should().Be("saved@example.com");
    }

    [Test]
    public async Task RollbackAsync_DiscardsUncommittedChanges()
    {
        var unitOfWork = new UnitOfWork(this.DbContext);
        var userDao = new UserDao(this.DbContext, this.UserManager, this.RoleManager);

        await unitOfWork.BeginTransactionAsync();
        await userDao.CreateAsync(
            new RegisterUserRequest
            {
                Email = "rolled-back@example.com",
                Password = "Password1",
            }
        );
        await unitOfWork.RollbackAsync();

        var loadedUser = await userDao.GetByEmailAsync("rolled-back@example.com");

        loadedUser.Should().BeNull();
    }
}
