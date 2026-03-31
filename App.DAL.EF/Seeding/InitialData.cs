namespace App.DAL.EF.Seeding;

public static class InitialData
{
    public static readonly string[] ContactTypes = [
        "email",
        "post",
        "phone"
    ];

    public static readonly string[] Roles = [
        "user",
        "admin"
    ];

    public static readonly (string email, string password, string[] roles)[] Users = [
        ("admin@taltech.ee", "Kala.12345", ["admin"]),
        ("user@taltech.ee", "Kala.12345", ["user"]),
    ];
    
}