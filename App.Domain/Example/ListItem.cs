using System.ComponentModel.DataAnnotations;
using App.Domain.Identity;
using Base.Contracts;
using Base.Domain;

namespace App.Domain;

public class ListItem :  BaseEntityAppUser
{
    [StringLength(128,MinimumLength = 1)]
    public string ItemDescription { get; set; } = default!;
 
    public LangStr Summary { get; set; } = default!;

    public bool IsDone { get; set; }

    public AppUser? AppUser { get; set; } 
}