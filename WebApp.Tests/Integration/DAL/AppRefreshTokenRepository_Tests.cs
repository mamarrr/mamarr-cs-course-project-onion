using App.DAL.Contracts;
using App.DAL.DTO.Identity;
using App.DAL.EF;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.DAL;

public class AppRefreshTokenRepository_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AppRefreshTokenRepository_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FindByTokenHashAsync_ReturnsPersistedRefreshToken()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var tokenHash = UniqueHash("active");
        var tokenId = await CreateRefreshTokenAsync(uow, tokenHash);

        var token = await uow.RefreshTokens.FindByTokenHashAsync(tokenHash);

        token.Should().NotBeNull();
        token!.Id.Should().Be(tokenId);
        token.AppUserId.Should().Be(TestUsers.CompanyAOwnerId);
        token.RefreshToken.Should().Be(tokenHash);
    }

    [Fact]
    public async Task RotateAsync_MovesCurrentTokenToPreviousToken()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var oldHash = UniqueHash("old");
        var newHash = UniqueHash("new");
        var oldExpiration = new DateTime(2026, 5, 12, 10, 0, 0, DateTimeKind.Utc);
        var newExpiration = new DateTime(2026, 5, 13, 10, 0, 0, DateTimeKind.Utc);
        var tokenId = await CreateRefreshTokenAsync(uow, oldHash, oldExpiration);

        var rotated = await uow.RefreshTokens.RotateAsync(tokenId, newHash, newExpiration);
        await uow.SaveChangesAsync(CancellationToken.None);

        rotated.Should().NotBeNull();
        rotated!.RefreshToken.Should().Be(newHash);
        rotated.PreviousRefreshToken.Should().Be(oldHash);
        rotated.PreviousExpirationDT.Should().Be(oldExpiration);
        var byPrevious = await uow.RefreshTokens.FindByPreviousTokenHashAsync(oldHash);
        byPrevious.Should().NotBeNull();

        var persisted = await db.RefreshTokens.AsNoTracking().SingleAsync(token => token.Id == tokenId);
        persisted.RefreshToken.Should().Be(newHash);
        persisted.PreviousRefreshToken.Should().Be(oldHash);
    }

    [Fact]
    public async Task RemoveMethods_DeleteByIdAndCurrentTokenHash()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var byIdHash = UniqueHash("remove-id");
        var byHash = UniqueHash("remove-hash");
        var byId = await CreateRefreshTokenAsync(uow, byIdHash);
        var byTokenHash = await CreateRefreshTokenAsync(uow, byHash);

        var removedById = await uow.RefreshTokens.RemoveByIdAsync(byId);
        var removedByHash = await uow.RefreshTokens.RemoveByTokenHashAsync(byHash);
        var missingById = await uow.RefreshTokens.RemoveByIdAsync(Guid.NewGuid());
        var missingByHash = await uow.RefreshTokens.RemoveByTokenHashAsync(UniqueHash("missing"));
        await uow.SaveChangesAsync(CancellationToken.None);

        removedById.Should().BeTrue();
        removedByHash.Should().BeTrue();
        missingById.Should().BeFalse();
        missingByHash.Should().BeFalse();
        var remaining = await db.RefreshTokens.AsNoTracking()
            .Where(token => token.Id == byId || token.Id == byTokenHash)
            .ToListAsync();
        remaining.Should().BeEmpty();
    }

    private static async Task<Guid> CreateRefreshTokenAsync(
        IAppUOW uow,
        string tokenHash,
        DateTime? expiration = null)
    {
        var id = Guid.NewGuid();
        uow.RefreshTokens.Add(new AppRefreshTokenDalDto
        {
            Id = id,
            AppUserId = TestUsers.CompanyAOwnerId,
            RefreshToken = tokenHash,
            ExpirationDT = expiration ?? DateTime.UtcNow.AddDays(7),
            PreviousExpirationDT = DateTime.UtcNow
        });
        await uow.SaveChangesAsync(CancellationToken.None);
        return id;
    }

    private static string UniqueHash(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}-{Guid.NewGuid():N}"[..64];
    }
}
