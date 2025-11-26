using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SpecmaticProvider;

/// <summary>
/// Generates JWT tokens for OAuth 2.0 testing
/// </summary>
public class TokenGenerator
{
    private readonly string _jwtSecret;

    public TokenGenerator(string jwtSecret)
    {
        _jwtSecret = jwtSecret;
    }

    /// <summary>
    /// Generate a JWT token with specified scopes
    /// </summary>
    public string GenerateToken(string clientId, string[] scopes, int expirationMinutes = 60)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtSecret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, clientId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("scope", string.Join(" ", scopes)),  // OAuth scopes
        };

        var token = new JwtSecurityToken(
            issuer: "http://localhost:5001",
            audience: "api",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
