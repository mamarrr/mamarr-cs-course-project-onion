using App.Domain;
using App.DAL.Contracts;
using AwesomeAssertions;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Unit.Domain;

public class LookupEntity_Tests
{
    public static TheoryData<ILookUpEntity, string> LookupEntities => new()
    {
        { new ManagementCompanyRole { Code = "OWNER", Label = TestLangStr.Create("Owner", "Omanik") }, "OWNER" },
        { new ManagementCompanyJoinRequestStatus { Code = "PENDING", Label = TestLangStr.Create("Pending", "Ootel") }, "PENDING" },
        { new ContactType { Code = "EMAIL", Label = TestLangStr.Create("Email", "E-post") }, "EMAIL" },
        { new CustomerRepresentativeRole { Code = "PRIMARY", Label = TestLangStr.Create("Primary", "Peamine") }, "PRIMARY" },
        { new PropertyType { Code = "APARTMENT_BUILDING", Label = TestLangStr.Create("Apartment building", "Korterelamu") }, "APARTMENT_BUILDING" },
        { new LeaseRole { Code = "TENANT", Label = TestLangStr.Create("Tenant", "Uurnik") }, "TENANT" },
        { new TicketCategory { Code = "PLUMBING", Label = TestLangStr.Create("Plumbing", "Torutood") }, "PLUMBING" },
        { new TicketPriority { Code = "MEDIUM", Label = TestLangStr.Create("Medium", "Keskmine") }, "MEDIUM" },
        { new TicketStatus { Code = "CREATED", Label = TestLangStr.Create("Created", "Loodud") }, "CREATED" },
        { new WorkStatus { Code = "SCHEDULED", Label = TestLangStr.Create("Scheduled", "Planeeritud") }, "SCHEDULED" }
    };

    [Theory]
    [MemberData(nameof(LookupEntities))]
    public void LookupEntity_PreservesStableCode(ILookUpEntity entity, string expectedCode)
    {
        entity.Code.Should().Be(expectedCode);
    }

    [Theory]
    [MemberData(nameof(LookupEntities))]
    public void LookupEntity_LabelSupportsMultipleCultures(ILookUpEntity entity, string _)
    {
        var en = entity.Label.Translate("en");
        var et = entity.Label.Translate("et");

        en.Should().NotBeNullOrWhiteSpace();
        et.Should().NotBeNullOrWhiteSpace();
        en.Should().NotBe(et);
    }

    [Fact]
    public void LookupEntity_LabelTranslationUpdate_PreservesOtherCultures()
    {
        var role = new ManagementCompanyRole
        {
            Code = "OWNER",
            Label = TestLangStr.Create("Owner", "Omanik")
        };

        role.Label.SetTranslation("Owner updated", "en");

        role.Label.Translate("en").Should().Be("Owner updated");
        role.Label.Translate("et").Should().Be("Omanik");
    }
}
