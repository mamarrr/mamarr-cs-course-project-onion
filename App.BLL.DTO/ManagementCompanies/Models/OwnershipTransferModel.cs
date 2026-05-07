namespace App.BLL.DTO.ManagementCompanies.Models;

public class OwnershipTransferModel
{
    public Guid PreviousOwnerMembershipId { get; set; }
    public Guid NewOwnerMembershipId { get; set; }
}
