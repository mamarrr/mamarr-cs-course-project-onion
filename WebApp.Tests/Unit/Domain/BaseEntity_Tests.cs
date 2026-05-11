using App.Domain;
using AwesomeAssertions;

namespace WebApp.Tests.Unit.Domain;

public class BaseEntity_Tests
{
    [Fact]
    public void NewEntity_HasNonEmptyGeneratedId()
    {
        var entity = new ManagementCompany();

        entity.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void CreatedAtMeta_DefaultsToDateTimeDefaultUntilSet()
    {
        var entity = new ManagementCompany();

        entity.CreatedAt.Should().Be(default);
    }

    [Fact]
    public void OptionalMetadata_IsNotAccidentallySetByDefault()
    {
        var user = new App.Domain.Identity.AppUser();

        user.LastLoginAt.Should().BeNull();
        user.ClosedAt.Should().BeNull();
    }

    [Fact]
    public void EntityInstances_DoNotCompareEqualBySharedIdAutomatically()
    {
        var id = Guid.NewGuid();
        var first = new ManagementCompany { Id = id };
        var second = new ManagementCompany { Id = id };

        first.Should().NotBeSameAs(second);
        first.Equals(second).Should().BeFalse();
    }
}
