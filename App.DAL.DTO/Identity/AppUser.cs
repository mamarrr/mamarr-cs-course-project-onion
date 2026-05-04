using Base.Contracts;
using Microsoft.AspNetCore.Identity;

namespace App.DAL.DTO.Identity;

public class AppUser: IdentityUser<Guid>, IBaseEntity
{
    public ICollection<ListItem>? ListItems { get; set; } 
}