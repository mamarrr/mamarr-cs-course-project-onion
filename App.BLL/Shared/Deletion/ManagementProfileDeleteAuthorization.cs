using App.BLL.Shared.Profiles;
using App.DAL.EF;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Shared.Deletion;

internal static class ManagementProfileDeleteAuthorization
{
    private static readonly HashSet<string> DeleteAllowedRoleCodes =
    [
        "OWNER",
        "MANAGER"
    ];

    public static async Task<bool> HasDeletePermissionAsync(
        AppDbContext dbContext,
        Guid managementCompanyId,
        Guid appUserId,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var roleCode = await dbContext.ManagementCompanyUsers
            .AsNoTracking()
            .Where(mcu => mcu.ManagementCompanyId == managementCompanyId && mcu.AppUserId == appUserId)
            .Where(mcu => mcu.IsActive)
            .Where(mcu => mcu.ValidFrom <= today)
            .Where(mcu => !mcu.ValidTo.HasValue || mcu.ValidTo.Value >= today)
            .Select(mcu => mcu.ManagementCompanyRole!.Code)
            .FirstOrDefaultAsync(cancellationToken);

        return roleCode != null && DeleteAllowedRoleCodes.Contains(roleCode.ToUpperInvariant());
    }

    public static ProfileOperationResult ForbiddenResult()
    {
        return new ProfileOperationResult
        {
            Forbidden = true,
            ErrorMessage = App.Resources.Views.UiText.AccessDeniedDescription
        };
    }
}
