namespace WebApp.Tests.Helpers;

public static class TestUsers
{
    public const string Password = "Test.pass1";

    public static readonly Guid UserAId = new("11111111-1111-1111-1111-111111111111");
    public const string UserAEmail = "user-a@test.ee";

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
}
