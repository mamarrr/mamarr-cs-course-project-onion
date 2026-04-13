using System.ComponentModel.DataAnnotations;
using Base.Domain;
using App.Domain;

namespace WebApp.ViewModels.ContactType;

public class ContactTypeDetailsViewModel
{
    public Guid Id { get; set; }
    
    [StringLength(20, MinimumLength = 1)]
    public string Code { get; set; } = default!;

    [Required]
    public string Label { get; set; } = default!;
}