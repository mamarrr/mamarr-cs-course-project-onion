namespace App.BLL.DTO.Admin.Lookups;

public enum AdminLookupType
{
    PropertyType,
    TicketCategory,
    TicketPriority,
    TicketStatus,
    WorkStatus,
    ContactType,
    ManagementCompanyRole
}

public class AdminLookupListDto
{
    public AdminLookupType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public IReadOnlyList<AdminLookupTypeOptionDto> LookupTypes { get; set; } = [];
    public IReadOnlyList<AdminLookupItemDto> Items { get; set; } = [];
}

public class AdminLookupTypeOptionDto
{
    public AdminLookupType Type { get; set; }
    public string Title { get; set; } = string.Empty;
}

public class AdminLookupItemDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsProtected { get; set; }
}

public class AdminLookupEditDto
{
    public Guid? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsProtected { get; set; }
}

public class AdminLookupDeleteCheckDto
{
    public AdminLookupType Type { get; set; }
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsProtected { get; set; }
    public bool IsInUse { get; set; }
    public string? BlockReason { get; set; }
}
