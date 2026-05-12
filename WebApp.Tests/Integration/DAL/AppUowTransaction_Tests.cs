using App.DAL.Contracts;
using App.DAL.DTO.Lookups;
using App.DAL.EF;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp.Tests.Integration.DAL;

public class AppUowTransaction_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AppUowTransaction_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CommitTransactionAsync_PersistsSavedChanges()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var code = UniqueCode("TX_COMMIT");

        await uow.BeginTransactionAsync();
        var created = await uow.Lookups.CreateLookupItemAsync(LookupTable.ContactType, code, "Committed contact type");
        await uow.SaveChangesAsync(CancellationToken.None);
        await uow.CommitTransactionAsync();

        var persisted = await uow.Lookups.FindLookupItemAsync(LookupTable.ContactType, created.Id);
        persisted.Should().NotBeNull();
        persisted!.Code.Should().Be(code);
        persisted.Label.Should().Be("Committed contact type");
    }

    [Fact]
    public async Task RollbackTransactionAsync_DiscardsSavedChanges()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var code = UniqueCode("TX_ROLLBACK");

        await uow.BeginTransactionAsync();
        var created = await uow.Lookups.CreateLookupItemAsync(LookupTable.ContactType, code, "Rolled back contact type");
        await uow.SaveChangesAsync(CancellationToken.None);
        await uow.RollbackTransactionAsync();

        db.ChangeTracker.Clear();
        var persisted = await uow.Lookups.FindLookupItemAsync(LookupTable.ContactType, created.Id);
        persisted.Should().BeNull();
    }

    [Fact]
    public async Task TransactionMethods_RejectInvalidStateTransitions()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        Func<Task> commitWithoutTransaction = () => uow.CommitTransactionAsync();
        await commitWithoutTransaction.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("No active transaction exists.");

        Func<Task> rollbackWithoutTransaction = () => uow.RollbackTransactionAsync();
        await rollbackWithoutTransaction.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("No active transaction exists.");

        await uow.BeginTransactionAsync();
        Func<Task> beginWhileActive = () => uow.BeginTransactionAsync();
        await beginWhileActive.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A transaction is already active.");
        await uow.RollbackTransactionAsync();
    }

    [Fact]
    public async Task TransactionCanBeStartedAgainAfterRollback()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        await uow.BeginTransactionAsync();
        await uow.RollbackTransactionAsync();

        await uow.BeginTransactionAsync();
        await uow.RollbackTransactionAsync();
    }

    private static string UniqueCode(string prefix)
    {
        return $"{prefix}_{Guid.NewGuid():N}"[..32].ToUpperInvariant();
    }
}
