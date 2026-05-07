namespace App.BLL.DTO.ManagementCompanies.Models;

public enum CompanyMembershipUserActionBlockReason
{
    None = 0,
    OwnerProtected,
    SelfProtected,
    OwnershipTransferRequired,
    RoleNotAssignable,
    MembershipNotEffective,
    InvalidDateRange,
    TargetNotEligibleForOwnership
}
