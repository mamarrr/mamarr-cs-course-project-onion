namespace App.DAL.EF.Seeding;

public static class InitialData
{

    public static readonly string[] Roles = [
        "SystemAdmin",
        "User"
    ];
    public static readonly (string email, string password, string FirstName, string LastName, string[] roles)[] Users = [
        ("admin@admin.ee", "Asd123!", "admin", "admin", ["SystemAdmin"]),
    ];

    public static readonly (string code, string en, string ee)[] ManagementCompanyRoleSeeds =
    [
        ("OWNER", "Owner", "Omanik"),
        ("MANAGER", "Manager", "Haldur"),
        ("SUPPORT", "Support specialist", "Tugispetsialist"),
        ("FINANCE", "Finance", "Finants")
    ];

    public static readonly (Guid id, string code, string en, string ee)[] ManagementCompanyJoinRequestStatusSeeds =
    [
        (Guid.Parse("11111111-1111-1111-1111-111111111111"), "PENDING", "Pending", "Ootel"),
        (Guid.Parse("22222222-2222-2222-2222-222222222222"), "APPROVED", "Approved", "Kinnitatud"),
        (Guid.Parse("33333333-3333-3333-3333-333333333333"), "REJECTED", "Rejected", "Tagasi lükatud")
    ];

    public static readonly (string code, string en, string ee)[] CustomerRepresentativeRoleSeeds =
    [
        ("PRIMARY", "Primary representative", "Peamine esindaja"),
        ("TECHNICAL", "Technical representative", "Tehniline esindaja"),
        ("BILLING", "Billing representative", "Arvelduse esindaja")
    ];

    public static readonly (string code, string en, string ee)[] ContactTypeSeeds =
    [
        ("EMAIL", "Email", "E-post"),
        ("PHONE", "Phone", "Telefon"),
        ("ADDRESS", "Address", "Aadress")
    ];

    public static readonly (string code, string en, string ee)[] PropertyTypeSeeds =
    [
        ("APARTMENT_BUILDING", "Apartment building", "Korterelamu"),
        ("PRIVATE_HOUSE", "Private house", "Eramu"),
        ("COMMERCIAL", "Commercial property", "Ärikinnisvara")
    ];

    public static readonly (string code, string en, string ee)[] LeaseRoleSeeds =
    [
        ("TENANT", "Tenant", "Üürnik"),
        ("OWNER", "Owner", "Omanik"),
        ("CO_TENANT", "Co-tenant", "Kaasüürnik")
    ];

    public static readonly (string code, string en, string ee)[] TicketCategorySeeds =
    [
        ("PLUMBING", "Plumbing", "Torutööd"),
        ("ELECTRICAL", "Electrical", "Elektritööd"),
        ("HVAC", "Heating and ventilation", "Küte ja ventilatsioon"),
        ("GENERAL", "General maintenance", "Üldhooldus")
    ];

    public static readonly (string code, string en, string ee)[] TicketStatusSeeds =
    [
        ("CREATED", "Created", "Loodud"),
        ("ASSIGNED", "Assigned", "Määratud"),
        ("SCHEDULED", "Scheduled", "Planeeritud"),
        ("IN_PROGRESS", "In progress", "Töös"),
        ("COMPLETED", "Completed", "Lõpetatud"),
        ("CLOSED", "Closed", "Suletud")
    ];

    public static readonly (string code, string en, string ee)[] TicketPrioritySeeds =
    [
        ("LOW", "Low", "Madal"),
        ("MEDIUM", "Medium", "Keskmine"),
        ("HIGH", "High", "Kõrge"),
        ("URGENT", "Urgent", "Kiire")
    ];

    public static readonly (string code, string en, string ee)[] WorkStatusSeeds =
    [
        ("SCHEDULED", "Scheduled", "Planeeritud"),
        ("IN_PROGRESS", "In progress", "Töös"),
        ("DONE", "Done", "Valmis"),
        ("CANCELLED", "Cancelled", "Tühistatud")
    ];

    
}
