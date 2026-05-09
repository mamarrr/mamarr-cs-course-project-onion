using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using App.Domain.Identity;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace WebApp.Services.Identity;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly UserManager<AppUser> _userManager;

    public JwtTokenService(
        IConfiguration configuration,
        UserManager<AppUser> userManager)
    {
        _configuration = configuration;
        _userManager = userManager;
    }

    public async Task<Result<JwtTokenResult>> CreateAccessTokenAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default)
    {
        var appUser = await _userManager.FindByIdAsync(appUserId.ToString());
        if (appUser == null)
        {
            return Result.Fail<JwtTokenResult>("User was not found.");
        }

        var roles = await _userManager.GetRolesAsync(appUser);
        var expiresAt = DateTime.UtcNow.AddSeconds(
            _configuration.GetValue<int?>("JWT:ExpiresInSeconds") ?? 1800);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, appUser.Id.ToString()),
            new(ClaimTypes.NameIdentifier, appUser.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, appUser.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.GivenName, appUser.FirstName),
            new(JwtRegisteredClaimNames.FamilyName, appUser.LastName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["JWT:Key"]!));
        var signingCredentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: _configuration["JWT:Issuer"],
            audience: _configuration["JWT:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: signingCredentials);

        return Result.Ok(new JwtTokenResult
        {
            Token = new JwtSecurityTokenHandler().WriteToken(jwt),
            ExpiresAt = expiresAt,
            User = new IdentityUserInfo
            {
                Id = appUser.Id,
                Email = appUser.Email ?? string.Empty,
                FirstName = appUser.FirstName,
                LastName = appUser.LastName,
                Roles = roles.ToList()
            }
        });
    }
}
