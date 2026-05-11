namespace WebApp.Tests.Helpers;

public static class TestUsers
{
    public const string Password = "Test.pass1";

    public static readonly Guid SystemAdminId = new("10000000-0000-0000-0000-000000000001");
    public const string SystemAdminEmail = "system-admin@test.ee";

    public static readonly Guid CompanyAOwnerId = new("10000000-0000-0000-0000-000000000002");
    public const string CompanyAOwnerEmail = "company-a-owner@test.ee";

    public static readonly Guid LockedUserId = new("10000000-0000-0000-0000-000000000003");
    public const string LockedUserEmail = "locked-user@test.ee";

    public static readonly Guid UserAId = CompanyAOwnerId;
    public const string UserAEmail = CompanyAOwnerEmail;

    public static readonly Guid UserBId = new("22222222-2222-2222-2222-222222222222");
    public const string UserBEmail = "user-b@test.ee";

    public static readonly Guid UserAItemId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid UserBItemId = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public const string UserAItemEnSummary = "user-a item summary EN";
    public const string UserAItemEtSummary = "user-a item summary ET";
    public const string UserBItemEnSummary = "user-b item summary EN";
    public const string UserBItemEtSummary = "user-b item summary ET";

    public const string UserAItemDescription = "user-a item";
    public const string UserBItemDescription = "user-b item";

    public static TestUser SystemAdmin => new(SystemAdminId, SystemAdminEmail, "System", "Admin", true);
    public static TestUser CompanyAOwner => new(CompanyAOwnerId, CompanyAOwnerEmail, "Company", "Owner", false);
    public static TestUser LockedUser => new(LockedUserId, LockedUserEmail, "Locked", "User", false, true);
}

public sealed record TestUser(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    bool IsSystemAdmin,
    bool IsLocked = false);

public static class TestTenants
{
    public static readonly Guid CompanyAId = new("20000000-0000-0000-0000-000000000001");
    public const string CompanyASlug = "company-a";
    public const string CompanyAName = "Company A Maintenance";

    public static readonly Guid PropertyTypeReferencedId = new("30000000-0000-0000-0000-000000000001");
    public static readonly Guid TicketPriorityReferencedId = new("30000000-0000-0000-0000-000000000002");
    public static readonly Guid TicketStatusCreatedId = new("30000000-0000-0000-0000-000000000003");
    public static readonly Guid TicketCategoryReferencedId = new("30000000-0000-0000-0000-000000000004");
    public static readonly Guid WorkStatusScheduledId = new("30000000-0000-0000-0000-000000000005");

    public static readonly Guid CustomerAId = new("40000000-0000-0000-0000-000000000001");
    public static readonly Guid PropertyAId = new("50000000-0000-0000-0000-000000000001");
    public static readonly Guid UnitAId = new("60000000-0000-0000-0000-000000000001");
    public static readonly Guid VendorAId = new("70000000-0000-0000-0000-000000000001");
    public static readonly Guid TicketAId = new("80000000-0000-0000-0000-000000000001");
}
