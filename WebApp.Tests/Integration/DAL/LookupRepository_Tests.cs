using App.DAL.Contracts;
using App.DAL.DTO.Lookups;
using App.DAL.EF;
using AwesomeAssertions;
using Base.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.DAL;

public class LookupRepository_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public LookupRepository_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        LangStr.DefaultCulture = "en";
    }

    [Fact]
    public async Task FindManagementCompanyRoleByCodeAsync_ReturnsSeededRole()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var role = await uow.Lookups.FindManagementCompanyRoleByCodeAsync("OWNER");

        role.Should().NotBeNull();
        role!.Code.Should().Be("OWNER");
        role.Label.Should().Be("Owner");
    }

    [Fact]
    public async Task AllManagementCompanyRolesAsync_ReturnsSeededRolesOrderedByCode()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var roles = await uow.Lookups.AllManagementCompanyRolesAsync();

        roles.Select(role => role.Code).Should().Contain(["FINANCE", "MANAGER", "OWNER", "SUPPORT"]);
        roles.Select(role => role.Code).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetLookupItemsAsync_ReturnsPropertyTypes()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var items = await uow.Lookups.GetLookupItemsAsync(LookupTable.PropertyType);

        items.Should().Contain(item => item.Code == "APARTMENT_BUILDING" && item.Label == "Apartment building");
    }

    [Fact]
    public async Task CreateLookupItemAsync_PersistsTrimmedCodeAndDefaultCultureLabel()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var code = UniqueCode("PT_CREATE");

        var created = await uow.Lookups.CreateLookupItemAsync(LookupTable.PropertyType, $" {code} ", " Custom type ");
        await uow.SaveChangesAsync(CancellationToken.None);

        created.Code.Should().Be(code);
        created.Label.Should().Be("Custom type");

        var found = await uow.Lookups.FindLookupItemAsync(LookupTable.PropertyType, created.Id);
        found.Should().NotBeNull();
        found!.Code.Should().Be(code);
        found.Label.Should().Be("Custom type");
    }

    [Fact]
    public async Task CodeExistsAsync_IsTrueForExistingCode_AndFalseWhenExceptingSameId()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var existing = await uow.Lookups.FindLookupItemAsync(LookupTable.PropertyType, TestTenants.PropertyTypeReferencedId);

        var exists = await uow.Lookups.CodeExistsAsync(LookupTable.PropertyType, "APARTMENT_BUILDING");
        var excepted = await uow.Lookups.CodeExistsAsync(LookupTable.PropertyType, "APARTMENT_BUILDING", existing!.Id);

        exists.Should().BeTrue();
        excepted.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateLookupItemAsync_TracksAndPersistsCodeAndLabelChange()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var code = UniqueCode("PT_UPDATE");
        var updatedCode = UniqueCode("PT_UPDATED");
        var created = await uow.Lookups.CreateLookupItemAsync(LookupTable.PropertyType, code, "Original label");
        await uow.SaveChangesAsync(CancellationToken.None);

        var updated = await uow.Lookups.UpdateLookupItemAsync(
            LookupTable.PropertyType,
            created.Id,
            $" {updatedCode} ",
            " Updated label ");
        await uow.SaveChangesAsync(CancellationToken.None);

        updated.Should().NotBeNull();
        updated!.Code.Should().Be(updatedCode);
        updated.Label.Should().Be("Updated label");

        db.ChangeTracker.Clear();
        var persisted = await uow.Lookups.FindLookupItemAsync(LookupTable.PropertyType, created.Id);
        persisted!.Code.Should().Be(updatedCode);
        persisted.Label.Should().Be("Updated label");
    }

    [Fact]
    public async Task UpdateLookupItemAsync_UpdatesCurrentCultureOnly_PreservesOtherTranslations()
    {
        using var culture = new CultureScope("en");
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var code = UniqueCode("PT_LANG");
        var created = await uow.Lookups.CreateLookupItemAsync(LookupTable.PropertyType, code, "English label");
        await uow.SaveChangesAsync(CancellationToken.None);

        var entity = await db.PropertyTypes.AsTracking().SingleAsync(type => type.Id == created.Id);
        entity.Label.SetTranslation("Eesti silt", "et");
        db.Entry(entity).Property(type => type.Label).IsModified = true;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        await uow.Lookups.UpdateLookupItemAsync(LookupTable.PropertyType, created.Id, code, "English updated");
        await uow.SaveChangesAsync(CancellationToken.None);

        var persisted = await db.PropertyTypes.AsNoTracking().SingleAsync(type => type.Id == created.Id);
        persisted.Label.Translate("en").Should().Be("English updated");
        persisted.Label.Translate("et").Should().Be("Eesti silt");
    }

    [Fact]
    public async Task IsLookupInUseAsync_DetectsReferencedPropertyType()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var inUse = await uow.Lookups.IsLookupInUseAsync(LookupTable.PropertyType, TestTenants.PropertyTypeReferencedId);

        inUse.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteLookupItemAsync_RemovesUnreferencedLookup()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var code = UniqueCode("PT_DELETE");
        var created = await uow.Lookups.CreateLookupItemAsync(LookupTable.PropertyType, code, "Delete me");
        await uow.SaveChangesAsync(CancellationToken.None);

        var deleted = await uow.Lookups.DeleteLookupItemAsync(LookupTable.PropertyType, created.Id);
        await uow.SaveChangesAsync(CancellationToken.None);

        deleted.Should().BeTrue();
        var found = await uow.Lookups.FindLookupItemAsync(LookupTable.PropertyType, created.Id);
        found.Should().BeNull();
    }

    private static string UniqueCode(string prefix)
    {
        return $"{prefix}_{Guid.NewGuid():N}"[..32].ToUpperInvariant();
    }
}
