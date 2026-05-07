using App.BLL.DTO.Admin.Lookups;
using FluentResults;

namespace App.BLL.Contracts.Admin;

public interface IAdminLookupService
{
    IReadOnlyList<AdminLookupTypeOptionDto> GetLookupTypes();
    Task<AdminLookupListDto> GetLookupItemsAsync(AdminLookupType type, CancellationToken cancellationToken = default);
    Task<AdminLookupEditDto?> GetLookupItemForEditAsync(AdminLookupType type, Guid id, CancellationToken cancellationToken = default);
    Task<Result<AdminLookupItemDto>> CreateLookupItemAsync(AdminLookupType type, AdminLookupEditDto dto, CancellationToken cancellationToken = default);
    Task<Result<AdminLookupItemDto>> UpdateLookupItemAsync(AdminLookupType type, Guid id, AdminLookupEditDto dto, CancellationToken cancellationToken = default);
    Task<AdminLookupDeleteCheckDto> GetDeleteCheckAsync(AdminLookupType type, Guid id, CancellationToken cancellationToken = default);
    Task<Result> DeleteLookupItemAsync(AdminLookupType type, Guid id, CancellationToken cancellationToken = default);
}
