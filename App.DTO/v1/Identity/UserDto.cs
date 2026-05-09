namespace App.DTO.v1.Identity;

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public IReadOnlyList<string> Roles { get; set; } = [];
}
