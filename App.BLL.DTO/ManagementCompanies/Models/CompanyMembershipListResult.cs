namespace App.BLL.DTO.ManagementCompanies.Models;

/// <summary>
/// Result of listing company members.
/// </summary>
public class CompanyMembershipListResult
{
    public IReadOnlyList<CompanyMembershipUserListItem> Members { get; set; } = Array.Empty<CompanyMembershipUserListItem>();
}
