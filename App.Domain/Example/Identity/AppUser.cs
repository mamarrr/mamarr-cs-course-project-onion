using Base.Contracts;
using Microsoft.AspNetCore.Identity;

namespace App.Domain.Identity;

public class AppUser : IdentityUser<Guid>, IBaseEntity
{
    public ICollection<ListItem>? ListItems { get; set; } 
    public ICollection<AppRefreshToken>? RefreshTokens { get; set; }
}