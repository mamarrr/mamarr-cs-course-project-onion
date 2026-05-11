using App.BLL.DTO.ManagementCompanies;
using App.DTO.v1.Mappers.Onboarding;
using App.DTO.v1.Mappers.Workspace;
using App.DTO.v1.Onboarding;
using Base.Contracts;

namespace WebApp.Extensions;

public static class ApiMapperServiceCollectionExtensions
{
    public static IServiceCollection AddApiMappers(this IServiceCollection services)
    {
        services.AddScoped<IBaseMapper<CreateManagementCompanyDto, ManagementCompanyBllDto>, ManagementCompanyApiMapper>();
        services.AddScoped<WorkspaceApiMapper>();

        return services;
    }
}
