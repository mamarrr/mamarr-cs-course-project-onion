namespace App.BLL.DTO.ManagementCompanies.Models;

public enum CompanyMembershipAuthorizationFailureReason
{
    None = 0,
    CompanyNotFound,
    MembershipNotFound,
    MembershipInactive,
    MembershipNotEffective,
    InsufficientPrivileges
}
