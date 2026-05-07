namespace App.BLL.DTO.Common.Routes;

public class ManagementCompanyRoute
{
    public Guid AppUserId { get; init; }
    public string CompanySlug { get; init; } = default!;
}

public class ContactRoute : ManagementCompanyRoute
{
    public Guid ContactId { get; init; }
}

public class CustomerRoute : ManagementCompanyRoute
{
    public string CustomerSlug { get; init; } = default!;
}

public class PropertyRoute : CustomerRoute
{
    public string PropertySlug { get; init; } = default!;
}

public class UnitRoute : PropertyRoute
{
    public string UnitSlug { get; init; } = default!;
}

public class ResidentRoute : ManagementCompanyRoute
{
    public string ResidentIdCode { get; init; } = default!;
}

public class ResidentContactRoute : ResidentRoute
{
    public Guid ResidentContactId { get; init; }
}

public class TicketRoute : ManagementCompanyRoute
{
    public Guid TicketId { get; init; }
}

public class ScheduledWorkRoute : TicketRoute
{
    public Guid ScheduledWorkId { get; init; }
}

public class VendorRoute : ManagementCompanyRoute
{
    public Guid VendorId { get; init; }
}

public class VendorCategoryRoute : VendorRoute
{
    public Guid TicketCategoryId { get; init; }
}

public class VendorContactRoute : VendorRoute
{
    public Guid VendorContactId { get; init; }
}

public class ResidentLeaseRoute : ResidentRoute
{
    public Guid LeaseId { get; init; }
}

public class UnitLeaseRoute : UnitRoute
{
    public Guid LeaseId { get; init; }
}

public class ManagementTicketSearchRoute : ManagementCompanyRoute
{
    public string? Search { get; init; }
    public Guid? StatusId { get; init; }
    public Guid? PriorityId { get; init; }
    public Guid? CategoryId { get; init; }
    public Guid? CustomerId { get; init; }
    public Guid? PropertyId { get; init; }
    public Guid? UnitId { get; init; }
    public Guid? VendorId { get; init; }
    public DateTime? DueFrom { get; init; }
    public DateTime? DueTo { get; init; }
}

public class TicketSelectorOptionsRoute : ManagementCompanyRoute
{
    public Guid? CustomerId { get; init; }
    public Guid? PropertyId { get; init; }
    public Guid? UnitId { get; init; }
    public Guid? CategoryId { get; init; }
}
