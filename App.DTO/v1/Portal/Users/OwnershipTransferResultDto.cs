namespace App.DTO.v1.Portal.Users;

public class OwnershipTransferResultDto
{
    public Guid PreviousOwnerMembershipId { get; set; }
    public Guid NewOwnerMembershipId { get; set; }
}
