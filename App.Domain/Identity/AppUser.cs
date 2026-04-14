using System.ComponentModel.DataAnnotations;
using Base.Contracts;
using Microsoft.AspNetCore.Identity;
using App.Domain;

namespace App.Domain.Identity;

public class AppUser : IdentityUser<Guid>, IBaseEntity
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string FirstName { get; set; } = default!;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string LastName { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime? ClosedAt { get; set; }

    public ICollection<AppRefreshToken>? RefreshTokens { get; set; }
    public ICollection<ManagementCompanyUser>? ManagementCompanyUsers { get; set; }
    public ICollection<ManagementCompanyJoinRequest>? ManagementCompanyJoinRequests { get; set; }
    public ICollection<ManagementCompanyJoinRequest>? ResolvedManagementCompanyJoinRequests { get; set; }
    public ICollection<ResidentUser>? ResidentUsers { get; set; }
    public ICollection<WorkLog>? WorkLogs { get; set; }
}
