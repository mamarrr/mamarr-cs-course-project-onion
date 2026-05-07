using App.BLL.Contracts.Admin;
using App.BLL.DTO.Admin.Lookups;
using App.BLL.DTO.Common.Errors;
using App.DAL.Contracts;
using App.DAL.DTO.Lookups;
using FluentResults;

namespace App.BLL.Services.Admin;

public class AdminLookupService : IAdminLookupService
{
    private static readonly IReadOnlyDictionary<AdminLookupType, string> Titles = new Dictionary<AdminLookupType, string>
    {
        [AdminLookupType.PropertyType] = "Property types",
        [AdminLookupType.TicketCategory] = "Ticket categories",
        [AdminLookupType.TicketPriority] = "Ticket priorities",
        [AdminLookupType.TicketStatus] = "Ticket statuses",
        [AdminLookupType.WorkStatus] = "Work statuses",
        [AdminLookupType.ContactType] = "Contact types",
        [AdminLookupType.ManagementCompanyRole] = "Management company roles"
    };

    private static readonly IReadOnlyDictionary<AdminLookupType, HashSet<string>> ProtectedCodes =
        new Dictionary<AdminLookupType, HashSet<string>>
        {
            [AdminLookupType.ManagementCompanyRole] = ["OWNER", "MANAGER", "SUPPORT", "FINANCE"],
            [AdminLookupType.TicketStatus] = ["CREATED", "ASSIGNED", "SCHEDULED", "IN_PROGRESS", "COMPLETED", "CLOSED"],
            [AdminLookupType.WorkStatus] = ["SCHEDULED", "IN_PROGRESS", "DONE", "CANCELLED"]
        };

    private readonly IAppUOW _uow;

    public AdminLookupService(IAppUOW uow)
    {
        _uow = uow;
    }

    public IReadOnlyList<AdminLookupTypeOptionDto> GetLookupTypes()
    {
        return Enum.GetValues<AdminLookupType>()
            .Select(type => new AdminLookupTypeOptionDto
            {
                Type = type,
                Title = Title(type)
            })
            .ToList();
    }

    public async Task<AdminLookupListDto> GetLookupItemsAsync(AdminLookupType type, CancellationToken cancellationToken = default)
    {
        var items = await _uow.Lookups.GetLookupItemsAsync(ToTable(type), cancellationToken);
        return new AdminLookupListDto
        {
            Type = type,
            Title = Title(type),
            LookupTypes = GetLookupTypes(),
            Items = items.Select(item => new AdminLookupItemDto
            {
                Id = item.Id,
                Code = item.Code,
                Label = item.Label,
                IsProtected = IsProtected(type, item.Code)
            }).ToList()
        };
    }

    public async Task<AdminLookupEditDto?> GetLookupItemForEditAsync(AdminLookupType type, Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _uow.Lookups.FindLookupItemAsync(ToTable(type), id, cancellationToken);
        return item is null
            ? null
            : new AdminLookupEditDto
            {
                Id = item.Id,
                Code = item.Code,
                Label = item.Label,
                IsProtected = IsProtected(type, item.Code)
            };
    }

    public async Task<Result<AdminLookupItemDto>> CreateLookupItemAsync(AdminLookupType type, AdminLookupEditDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(type, dto, null, cancellationToken);
        if (validation.IsFailed)
        {
            return Result.Fail<AdminLookupItemDto>(validation.Errors);
        }

        var code = dto.Code.Trim();
        var label = dto.Label.Trim();
        var created = await _uow.Lookups.CreateLookupItemAsync(ToTable(type), code, label, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Ok(Map(type, created.Id, created.Code, created.Label));
    }

    public async Task<Result<AdminLookupItemDto>> UpdateLookupItemAsync(AdminLookupType type, Guid id, AdminLookupEditDto dto, CancellationToken cancellationToken = default)
    {
        var existing = await _uow.Lookups.FindLookupItemAsync(ToTable(type), id, cancellationToken);
        if (existing is null)
        {
            return Result.Fail<AdminLookupItemDto>(new NotFoundError("Lookup item was not found."));
        }

        if (IsProtected(type, existing.Code) && !string.Equals(existing.Code, dto.Code.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return Result.Fail<AdminLookupItemDto>(new BusinessRuleError("Protected lookup codes cannot be changed."));
        }

        var validation = await ValidateAsync(type, dto, id, cancellationToken);
        if (validation.IsFailed)
        {
            return Result.Fail<AdminLookupItemDto>(validation.Errors);
        }

        var code = dto.Code.Trim();
        var label = dto.Label.Trim();
        var updated = await _uow.Lookups.UpdateLookupItemAsync(ToTable(type), id, code, label, cancellationToken);
        if (updated is null)
        {
            return Result.Fail<AdminLookupItemDto>(new NotFoundError("Lookup item was not found."));
        }

        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Ok(Map(type, updated.Id, updated.Code, updated.Label));
    }

    public async Task<AdminLookupDeleteCheckDto> GetDeleteCheckAsync(AdminLookupType type, Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _uow.Lookups.FindLookupItemAsync(ToTable(type), id, cancellationToken);
        if (item is null)
        {
            return new AdminLookupDeleteCheckDto
            {
                Type = type,
                Id = id,
                BlockReason = "Lookup item was not found."
            };
        }

        var protectedCode = IsProtected(type, item.Code);
        var inUse = await _uow.Lookups.IsLookupInUseAsync(ToTable(type), id, cancellationToken);
        return new AdminLookupDeleteCheckDto
        {
            Type = type,
            Id = id,
            Code = item.Code,
            Label = item.Label,
            IsProtected = protectedCode,
            IsInUse = inUse,
            BlockReason = protectedCode
                ? "Protected lookup values cannot be deleted."
                : inUse
                    ? "Lookup value is in use and cannot be deleted."
                    : null
        };
    }

    public async Task<Result> DeleteLookupItemAsync(AdminLookupType type, Guid id, CancellationToken cancellationToken = default)
    {
        var check = await GetDeleteCheckAsync(type, id, cancellationToken);
        if (!string.IsNullOrWhiteSpace(check.BlockReason))
        {
            return Result.Fail(new BusinessRuleError(check.BlockReason));
        }

        if (!await _uow.Lookups.DeleteLookupItemAsync(ToTable(type), id, cancellationToken))
        {
            return Result.Fail(new NotFoundError("Lookup item was not found."));
        }

        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    private async Task<Result> ValidateAsync(AdminLookupType type, AdminLookupEditDto dto, Guid? id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.Label))
        {
            return Result.Fail(new ValidationAppError("Code and label are required.", []));
        }

        if (await _uow.Lookups.CodeExistsAsync(ToTable(type), dto.Code, id, cancellationToken))
        {
            return Result.Fail(new ConflictError("Lookup code already exists."));
        }

        return Result.Ok();
    }

    private static AdminLookupItemDto Map(AdminLookupType type, Guid id, string code, string label)
    {
        return new AdminLookupItemDto
        {
            Id = id,
            Code = code,
            Label = label,
            IsProtected = IsProtected(type, code)
        };
    }

    private static string Title(AdminLookupType type)
    {
        return Titles.TryGetValue(type, out var title) ? title : type.ToString();
    }

    private static bool IsProtected(AdminLookupType type, string code)
    {
        return ProtectedCodes.TryGetValue(type, out var codes) && codes.Contains(code);
    }

    private static LookupTable ToTable(AdminLookupType type)
    {
        return type switch
        {
            AdminLookupType.PropertyType => LookupTable.PropertyType,
            AdminLookupType.TicketCategory => LookupTable.TicketCategory,
            AdminLookupType.TicketPriority => LookupTable.TicketPriority,
            AdminLookupType.TicketStatus => LookupTable.TicketStatus,
            AdminLookupType.WorkStatus => LookupTable.WorkStatus,
            AdminLookupType.ContactType => LookupTable.ContactType,
            AdminLookupType.ManagementCompanyRole => LookupTable.ManagementCompanyRole,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}
