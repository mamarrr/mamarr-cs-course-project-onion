using App.BLL.Contracts;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.ManagementCompanies.Models;
using App.DTO.v1.Common;
using App.DTO.v1.Mappers.Portal.Users;
using App.DTO.v1.Portal.Users;
using Asp.Versioning;
using Base.Contracts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.Portal;

[ApiVersion("1.0")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/v{version:apiVersion}/portal/companies/{companySlug}/users")]
public class CompanyUsersController : ApiControllerBase
{
    private readonly IAppBLL _bll;
    private readonly IBaseMapper<AddCompanyUserDto, CompanyMembershipAddRequest> _addMapper;
    private readonly IBaseMapper<UpdateCompanyUserDto, CompanyMembershipUpdateRequest> _updateMapper;
    private readonly IBaseMapper<TransferOwnershipDto, TransferOwnershipRequest> _transferMapper;
    private readonly CompanyUserApiMapper _userMapper;
    private readonly PendingAccessRequestApiMapper _pendingMapper = new();
    private readonly OwnershipTransferApiMapper _ownershipMapper;

    public CompanyUsersController(IAppBLL bll)
    {
        _bll = bll;
        _userMapper = new CompanyUserApiMapper();
        _ownershipMapper = new OwnershipTransferApiMapper();
        _addMapper = _userMapper;
        _updateMapper = _userMapper;
        _transferMapper = _ownershipMapper;
    }

    [HttpGet]
    [ProducesResponseType(typeof(CompanyUsersPageDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CompanyUsersPageDto>> List(
        string companySlug,
        CancellationToken cancellationToken)
    {
        var (context, error) = await AuthorizeCompanyAdminAsync(companySlug, cancellationToken);
        if (error is not null)
        {
            return error;
        }

        var membersResult = await _bll.CompanyMemberships.ListCompanyMembersAsync(context!, cancellationToken);
        if (membersResult.IsFailed)
        {
            return ToApiError(membersResult.Errors);
        }

        var pendingResult = await _bll.CompanyMemberships.GetPendingAccessRequestsAsync(context!, cancellationToken);
        if (pendingResult.IsFailed)
        {
            return ToApiError(pendingResult.Errors);
        }

        var rolesResult = await _bll.CompanyMemberships.GetAddRoleOptionsAsync(context!, cancellationToken);
        if (rolesResult.IsFailed)
        {
            return ToApiError(rolesResult.Errors);
        }

        return Ok(_userMapper.Map(
            context!,
            membersResult.Value,
            pendingResult.Value,
            rolesResult.Value,
            _pendingMapper));
    }

    [HttpPost]
    [ProducesResponseType(typeof(CompanyUserEditDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CompanyUserEditDto>> Add(
        string companySlug,
        AddCompanyUserDto? dto,
        CancellationToken cancellationToken)
    {
        var (context, error) = await AuthorizeCompanyAdminAsync(companySlug, cancellationToken);
        if (error is not null)
        {
            return error;
        }

        var request = _addMapper.Map(dto);
        if (request is null)
        {
            return InvalidRequest("Company user payload is required.");
        }

        var addResult = await _bll.CompanyMemberships.AddUserByEmailAsync(
            context!,
            request,
            cancellationToken);

        if (addResult.IsFailed)
        {
            return ToApiError(addResult.Errors);
        }

        var editResult = await _bll.CompanyMemberships.GetMembershipForEditAsync(
            context!,
            addResult.Value,
            cancellationToken);

        if (editResult.IsFailed)
        {
            return ToApiError(editResult.Errors);
        }

        return CreatedAtAction(
            nameof(Get),
            new
            {
                version = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1",
                companySlug,
                membershipId = addResult.Value
            },
            _userMapper.Map(editResult.Value, context!));
    }

    [HttpGet("{membershipId:guid}")]
    [ProducesResponseType(typeof(CompanyUserEditDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CompanyUserEditDto>> Get(
        string companySlug,
        Guid membershipId,
        CancellationToken cancellationToken)
    {
        var (context, error) = await AuthorizeCompanyAdminAsync(companySlug, cancellationToken);
        if (error is not null)
        {
            return error;
        }

        var result = await _bll.CompanyMemberships.GetMembershipForEditAsync(
            context!,
            membershipId,
            cancellationToken);

        return result.IsFailed
            ? ToApiError(result.Errors)
            : Ok(_userMapper.Map(result.Value, context!));
    }

    [HttpPut("{membershipId:guid}")]
    [ProducesResponseType(typeof(CompanyUserEditDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CompanyUserEditDto>> Update(
        string companySlug,
        Guid membershipId,
        UpdateCompanyUserDto? dto,
        CancellationToken cancellationToken)
    {
        var (context, error) = await AuthorizeCompanyAdminAsync(companySlug, cancellationToken);
        if (error is not null)
        {
            return error;
        }

        var request = _updateMapper.Map(dto);
        if (request is null)
        {
            return InvalidRequest("Company user payload is required.");
        }

        var updateResult = await _bll.CompanyMemberships.UpdateMembershipAsync(
            context!,
            membershipId,
            request,
            cancellationToken);

        if (updateResult.IsFailed)
        {
            return ToApiError(updateResult.Errors);
        }

        var editResult = await _bll.CompanyMemberships.GetMembershipForEditAsync(
            context!,
            membershipId,
            cancellationToken);

        return editResult.IsFailed
            ? ToApiError(editResult.Errors)
            : Ok(_userMapper.Map(editResult.Value, context!));
    }

    [HttpDelete("{membershipId:guid}")]
    [ProducesResponseType(typeof(CommandResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CommandResultDto>> Delete(
        string companySlug,
        Guid membershipId,
        CancellationToken cancellationToken)
    {
        var (context, error) = await AuthorizeCompanyAdminAsync(companySlug, cancellationToken);
        if (error is not null)
        {
            return error;
        }

        var result = await _bll.CompanyMemberships.DeleteMembershipAsync(
            context!,
            membershipId,
            cancellationToken);

        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        return Ok(new CommandResultDto
        {
            Success = true,
            Message = "Company user removed."
        });
    }

    [HttpGet("roles")]
    [ProducesResponseType(typeof(IReadOnlyList<CompanyUserRoleOptionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CompanyUserRoleOptionDto>>> Roles(
        string companySlug,
        CancellationToken cancellationToken)
    {
        var (context, error) = await AuthorizeCompanyAdminAsync(companySlug, cancellationToken);
        if (error is not null)
        {
            return error;
        }

        var result = await _bll.CompanyMemberships.GetAddRoleOptionsAsync(context!, cancellationToken);
        return result.IsFailed
            ? ToApiError(result.Errors)
            : Ok(result.Value.Select(_userMapper.Map).OfType<CompanyUserRoleOptionDto>().ToList());
    }

    [HttpGet("ownership-transfer-candidates")]
    [ProducesResponseType(typeof(IReadOnlyList<OwnershipTransferCandidateDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OwnershipTransferCandidateDto>>> OwnershipTransferCandidates(
        string companySlug,
        CancellationToken cancellationToken)
    {
        var (context, error) = await AuthorizeCompanyAdminAsync(companySlug, cancellationToken);
        if (error is not null)
        {
            return error;
        }

        var result = await _bll.CompanyMemberships.GetOwnershipTransferCandidatesAsync(
            context!,
            cancellationToken);

        return result.IsFailed
            ? ToApiError(result.Errors)
            : Ok(result.Value.Select(_ownershipMapper.Map).OfType<OwnershipTransferCandidateDto>().ToList());
    }

    [HttpPost("transfer-ownership")]
    [ProducesResponseType(typeof(OwnershipTransferResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OwnershipTransferResultDto>> TransferOwnership(
        string companySlug,
        TransferOwnershipDto? dto,
        CancellationToken cancellationToken)
    {
        var (context, error) = await AuthorizeCompanyAdminAsync(companySlug, cancellationToken);
        if (error is not null)
        {
            return error;
        }

        var request = _transferMapper.Map(dto);
        if (request is null)
        {
            return InvalidRequest("Ownership transfer payload is required.");
        }

        var result = await _bll.CompanyMemberships.TransferOwnershipAsync(
            context!,
            request,
            cancellationToken);

        return result.IsFailed
            ? ToApiError(result.Errors)
            : Ok(_ownershipMapper.Map(result.Value));
    }

    [HttpPost("access-requests/{requestId:guid}/approve")]
    [ProducesResponseType(typeof(CommandResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CommandResultDto>> ApproveAccessRequest(
        string companySlug,
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var (context, error) = await AuthorizeCompanyAdminAsync(companySlug, cancellationToken);
        if (error is not null)
        {
            return error;
        }

        var result = await _bll.CompanyMemberships.ApprovePendingAccessRequestAsync(
            context!,
            requestId,
            cancellationToken);

        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        return Ok(new CommandResultDto
        {
            Success = true,
            Message = "Access request approved."
        });
    }

    [HttpPost("access-requests/{requestId:guid}/reject")]
    [ProducesResponseType(typeof(CommandResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CommandResultDto>> RejectAccessRequest(
        string companySlug,
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var (context, error) = await AuthorizeCompanyAdminAsync(companySlug, cancellationToken);
        if (error is not null)
        {
            return error;
        }

        var result = await _bll.CompanyMemberships.RejectPendingAccessRequestAsync(
            context!,
            requestId,
            cancellationToken);

        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        return Ok(new CommandResultDto
        {
            Success = true,
            Message = "Access request rejected."
        });
    }

    private async Task<(CompanyAdminAuthorizedContext? Context, ActionResult? Error)> AuthorizeCompanyAdminAsync(
        string companySlug,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return (null, UnauthorizedRequest("Authentication is required."));
        }

        var result = await _bll.CompanyMemberships.AuthorizeAsync(
            new ManagementCompanyRoute
            {
                AppUserId = appUserId.Value,
                CompanySlug = companySlug
            },
            cancellationToken);

        return result.IsFailed
            ? (null, ToApiError(result.Errors))
            : (result.Value, null);
    }
}
