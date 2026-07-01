using Microsoft.IdentityModel.Tokens;
using SpokenEnglishAPI.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SpokenEnglishAPI.Infrastructure.Security;

public class JwtTokenGenerator
{
    private readonly IConfiguration _config;

    public JwtTokenGenerator(IConfiguration config)
    {
        _config = config;
    }

    //public string Generate(User user)
    //{
    //    var claims = new[]
    //    {
    //        new Claim(ClaimTypes.NameIdentifier, user.UserGuid.ToString()),
    //        new Claim(ClaimTypes.Email, user.Email)
    //    };

    //    var key = new SymmetricSecurityKey(
    //        Encoding.UTF8.GetBytes(_config["Jwt:Key"]));

    //    var token = new JwtSecurityToken(
    //        issuer: _config["Jwt:Issuer"],
    //        audience: _config["Jwt:Audience"],
    //        claims: claims,
    //        expires: DateTime.UtcNow.AddMinutes(60),
    //        signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

    //    return new JwtSecurityTokenHandler().WriteToken(token);
    //}
    public string Generate(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserGuid.ToString()),
            new Claim("uid", user.ID.ToString()),   // integer id for ownership checks (IDOR guard)
            new Claim(ClaimTypes.Role, user.Role ?? "User"),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? "")
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"])
        );

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_config["Jwt:ExpiryMinutes"] ?? "30")),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
        => Guid.NewGuid().ToString();


}
