using App.DAL.EF;
using AwesomeAssertions;
using Base.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using DomainProperty = App.Domain.Property;

namespace WebApp.Tests.Integration.DAL;

public class AppDbContextModel_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AppDbContextModel_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void DbContext_UsesProductionLikeNoTrackingIdentityResolution()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.ChangeTracker.QueryTrackingBehavior.Should().Be(QueryTrackingBehavior.NoTrackingWithIdentityResolution);
    }

    [Fact]
    public void SlugProperties_AreRequiredAndLimitedTo128Characters()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        AssertRequiredSlug<App.Domain.ManagementCompany>(db);
        AssertRequiredSlug<App.Domain.Customer>(db);
        AssertRequiredSlug<DomainProperty>(db);
        AssertRequiredSlug<App.Domain.Unit>(db);
    }

    [Fact]
    public void LookupCodes_HaveUniqueIndexes()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        AssertUniqueIndex<App.Domain.ManagementCompanyRole>(db, nameof(App.Domain.ManagementCompanyRole.Code));
        AssertUniqueIndex<App.Domain.ManagementCompanyJoinRequestStatus>(db, nameof(App.Domain.ManagementCompanyJoinRequestStatus.Code));
        AssertUniqueIndex<App.Domain.ContactType>(db, nameof(App.Domain.ContactType.Code));
        AssertUniqueIndex<App.Domain.CustomerRepresentativeRole>(db, nameof(App.Domain.CustomerRepresentativeRole.Code));
        AssertUniqueIndex<App.Domain.PropertyType>(db, nameof(App.Domain.PropertyType.Code));
        AssertUniqueIndex<App.Domain.TicketCategory>(db, nameof(App.Domain.TicketCategory.Code));
        AssertUniqueIndex<App.Domain.WorkStatus>(db, nameof(App.Domain.WorkStatus.Code));
    }

    [Fact]
    public void TenantScopedNaturalKeys_HaveUniqueIndexes()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        AssertUniqueIndex<App.Domain.Customer>(db, nameof(App.Domain.Customer.ManagementCompanyId), nameof(App.Domain.Customer.RegistryCode));
        AssertUniqueIndex<App.Domain.Customer>(db, nameof(App.Domain.Customer.ManagementCompanyId), nameof(App.Domain.Customer.Slug));
        AssertUniqueIndex<App.Domain.Resident>(db, nameof(App.Domain.Resident.ManagementCompanyId), nameof(App.Domain.Resident.IdCode));
        AssertUniqueIndex<App.Domain.Vendor>(db, nameof(App.Domain.Vendor.ManagementCompanyId), nameof(App.Domain.Vendor.RegistryCode));
        AssertUniqueIndex<App.Domain.Ticket>(db, nameof(App.Domain.Ticket.ManagementCompanyId), nameof(App.Domain.Ticket.TicketNr));
        AssertUniqueIndex<App.Domain.Unit>(db, nameof(App.Domain.Unit.PropertyId), nameof(App.Domain.Unit.Slug));
        AssertUniqueIndex<App.Domain.Unit>(db, nameof(App.Domain.Unit.PropertyId), nameof(App.Domain.Unit.UnitNr));
    }

    [Fact]
    public void LangStrProperties_HaveJsonValueConverters()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        AssertLangStrProperty<App.Domain.PropertyType>(db, nameof(App.Domain.PropertyType.Label), nullable: false);
        AssertLangStrProperty<DomainProperty>(db, nameof(DomainProperty.Label), nullable: false);
        AssertLangStrProperty<DomainProperty>(db, nameof(DomainProperty.Notes), nullable: true);
        AssertLangStrProperty<App.Domain.Ticket>(db, nameof(App.Domain.Ticket.Title), nullable: false);
        AssertLangStrProperty<App.Domain.Ticket>(db, nameof(App.Domain.Ticket.Description), nullable: false);
        AssertLangStrProperty<App.Domain.WorkLog>(db, nameof(App.Domain.WorkLog.Description), nullable: true);
    }

    [Fact]
    public void DomainRelationships_DoNotCascadeDelete()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cascadingForeignKeys = db.Model.GetEntityTypes()
            .Where(entityType => entityType.ClrType.Namespace == "App.Domain")
            .SelectMany(entityType => entityType.GetForeignKeys())
            .Where(foreignKey => foreignKey.DeleteBehavior is DeleteBehavior.Cascade or DeleteBehavior.ClientCascade)
            .Select(foreignKey => $"{foreignKey.DeclaringEntityType.ClrType.Name}.{string.Join("_", foreignKey.Properties.Select(property => property.Name))}")
            .ToList();

        cascadingForeignKeys.Should().BeEmpty();
    }

    private static void AssertRequiredSlug<TEntity>(AppDbContext db)
        where TEntity : class
    {
        var property = GetProperty<TEntity>(db, "Slug");

        property.IsNullable.Should().BeFalse();
        property.GetMaxLength().Should().Be(128);
    }

    private static void AssertUniqueIndex<TEntity>(AppDbContext db, params string[] propertyNames)
        where TEntity : class
    {
        var entityType = db.Model.FindEntityType(typeof(TEntity));
        entityType.Should().NotBeNull();

        var index = entityType!.GetIndexes()
            .SingleOrDefault(candidate => candidate.Properties.Select(property => property.Name).SequenceEqual(propertyNames));

        index.Should().NotBeNull();
        index!.IsUnique.Should().BeTrue();
    }

    private static void AssertLangStrProperty<TEntity>(AppDbContext db, string propertyName, bool nullable)
        where TEntity : class
    {
        var property = GetProperty<TEntity>(db, propertyName);

        property.ClrType.Should().Be(typeof(LangStr));
        property.IsNullable.Should().Be(nullable);
        property.GetColumnType().Should().Be("jsonb");
        property.GetTypeMapping().Converter.Should().NotBeNull();
    }

    private static IProperty GetProperty<TEntity>(AppDbContext db, string propertyName)
        where TEntity : class
    {
        var entityType = db.Model.FindEntityType(typeof(TEntity));
        entityType.Should().NotBeNull();

        var property = entityType!.FindProperty(propertyName);
        property.Should().NotBeNull();
        return property!;
    }
}
