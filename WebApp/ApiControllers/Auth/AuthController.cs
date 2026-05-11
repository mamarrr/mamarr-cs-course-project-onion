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
        if (string.IsNullOrWhiteSpace(registerInfo.Email)
            || string.IsNullOrWhiteSpace(registerInfo.Password)
            || string.IsNullOrWhiteSpace(registerInfo.FirstName)
            || string.IsNullOrWhiteSpace(registerInfo.LastName))
        {
            return InvalidRequest("Email, password, first name, and last name are required.");
        }

        var createResult = await _identityAccountService.CreateUserAsync(
            registerInfo.Email,
            registerInfo.Password,
            registerInfo.FirstName,
            registerInfo.LastName,
            cancellationToken);

        if (createResult.IsFailed)
        {
            return ToApiError(createResult.Errors);
        }

        var appUserId = await _identityAccountService.FindUserIdByEmailAsync(
            registerInfo.Email,
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
}
