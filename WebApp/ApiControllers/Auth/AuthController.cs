using System.ComponentModel.DataAnnotations;
using App.BLL.DTO.Common;
using App.BLL.DTO.Common.Errors;
using App.BLL.Contracts;
using App.DTO.v1.Identity;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Services.Identity;

namespace WebApp.ApiControllers.Auth;

[ApiVersion("1.0")]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController : ApiControllerBase
{
    private readonly IAppBLL _bll;
    private readonly IIdentityAccountService _identityAccountService;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(
        IAppBLL bll,
        IIdentityAccountService identityAccountService,
        IJwtTokenService jwtTokenService)
    {
        _bll = bll;
        _identityAccountService = identityAccountService;
        _jwtTokenService = jwtTokenService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> Register(
        RegisterInfo registerInfo,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateRegisterInfo(registerInfo);
        if (validationError is not null)
        {
            return ToApiError([validationError]);
        }

        var email = registerInfo.Email.Trim();

        var createResult = await _identityAccountService.CreateUserAsync(
            email,
            registerInfo.Password,
            registerInfo.FirstName,
            registerInfo.LastName,
            cancellationToken);

        if (createResult.IsFailed)
        {
            return ToApiError(createResult.Errors);
        }

        var appUserId = await _identityAccountService.FindUserIdByEmailAsync(
            email,
            cancellationToken);

        if (appUserId is null)
        {
            return InvalidRequest("User registration failed.");
        }

        var userResult = await _identityAccountService.GetUserInfoAsync(appUserId.Value, cancellationToken);
        if (userResult.IsFailed)
        {
            return ToApiError(userResult.Errors);
        }

        return CreatedAtAction(
            nameof(Me),
            new { version = "1" },
            MapUser(userResult.Value));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(JWTResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<JWTResponse>> Login(
        LoginInfo loginInfo,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(loginInfo.Email) || string.IsNullOrWhiteSpace(loginInfo.Password))
        {
            return InvalidRequest("Email and password are required.");
        }

        var credentials = await _identityAccountService.ValidateCredentialsAsync(
            loginInfo.Email,
            loginInfo.Password,
            cancellationToken);

        if (credentials.IsFailed)
        {
            return UnauthorizedRequest(
                credentials.Errors.FirstOrDefault()?.Message
                ?? App.Resources.Views.UiText.InvalidEmailOrPassword);
        }

        return await IssueTokenResponseAsync(credentials.Value, cancellationToken);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(JWTResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<JWTResponse>> Refresh(
        TokenRefreshInfo refreshInfo,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshInfo.RefreshToken))
        {
            return InvalidRequest("Refresh token is required.");
        }

        var session = await _bll.AuthSessions.RotateSessionAsync(
            refreshInfo.RefreshToken,
            cancellationToken);

        if (session.IsFailed)
        {
            return ToApiError(session.Errors);
        }

        return await IssueTokenResponseAsync(
            session.Value.AppUserId,
            session.Value.RefreshToken,
            session.Value.ExpiresAt,
            cancellationToken);
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(
        LogoutInfo logoutInfo,
        CancellationToken cancellationToken)
    {
        await _bll.AuthSessions.RevokeSessionAsync(logoutInfo.RefreshToken, cancellationToken);
        return NoContent();
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> Me(CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return Unauthorized();
        }

        var user = await _identityAccountService.GetUserInfoAsync(appUserId.Value, cancellationToken);
        return user.IsFailed
            ? ToApiError(user.Errors)
            : Ok(MapUser(user.Value));
    }

    private async Task<ActionResult<JWTResponse>> IssueTokenResponseAsync(
        Guid appUserId,
        CancellationToken cancellationToken)
    {
        var session = await _bll.AuthSessions.CreateSessionAsync(appUserId, cancellationToken);
        if (session.IsFailed)
        {
            return ToApiError(session.Errors);
        }

        return await IssueTokenResponseAsync(
            appUserId,
            session.Value.RefreshToken,
            session.Value.ExpiresAt,
            cancellationToken);
    }

    private async Task<ActionResult<JWTResponse>> IssueTokenResponseAsync(
        Guid appUserId,
        string refreshToken,
        DateTime refreshTokenExpiresAt,
        CancellationToken cancellationToken)
    {
        var jwt = await _jwtTokenService.CreateAccessTokenAsync(appUserId, cancellationToken);
        if (jwt.IsFailed)
        {
            return ToApiError(jwt.Errors);
        }

        return Ok(new JWTResponse
        {
            Jwt = jwt.Value.Token,
            RefreshToken = refreshToken
        });
    }

    private static UserDto MapUser(IdentityUserInfo user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = user.Roles
        };
    }

    private static ValidationAppError? ValidateRegisterInfo(RegisterInfo registerInfo)
    {
        var failures = new List<ValidationFailureModel>();

        AddRequiredFailure(
            failures,
            registerInfo.Email,
            nameof(registerInfo.Email),
            App.Resources.Views.UiText.Email);

        AddRequiredFailure(
            failures,
            registerInfo.Password,
            nameof(registerInfo.Password),
            App.Resources.Views.UiText.Password);

        AddRequiredFailure(
            failures,
            registerInfo.FirstName,
            nameof(registerInfo.FirstName),
            App.Resources.Views.UiText.FirstName);

        AddRequiredFailure(
            failures,
            registerInfo.LastName,
            nameof(registerInfo.LastName),
            App.Resources.Views.UiText.LastName);

        if (!string.IsNullOrWhiteSpace(registerInfo.Email)
            && !new EmailAddressAttribute().IsValid(registerInfo.Email.Trim()))
        {
            failures.Add(new ValidationFailureModel
            {
                PropertyName = nameof(registerInfo.Email),
                ErrorMessage = string.Format(
                    App.Resources.Views.UiText.InvalidEmailAddress,
                    App.Resources.Views.UiText.Email)
            });
        }

        return failures.Count == 0
            ? null
            : new ValidationAppError("Validation failed.", failures);
    }

    private static void AddRequiredFailure(
        List<ValidationFailureModel> failures,
        string? value,
        string propertyName,
        string displayName)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        failures.Add(new ValidationFailureModel
        {
            PropertyName = propertyName,
            ErrorMessage = string.Format(App.Resources.Views.UiText.RequiredField, displayName)
        });
    }
}
