﻿using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace stpp.Auth
{
    public class JwtTokenService
    {
        private readonly SymmetricSecurityKey _authSigningKey;
        private readonly string _issuer;
        private readonly string _audience;
        public JwtTokenService(IConfiguration configuration)
        {
            _authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]));
            _audience = configuration["Jwt:ValidAudience"];
            _issuer = configuration["Jwt:ValidIssuer"];
        }

        public string CreateAccessToken(string userName, String userId, IEnumerable<string> roles)
        {
            var authClaims = new List<Claim>()
            {
                new(ClaimTypes.Name, userName),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Sub, userId)
            };
            authClaims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var token = new JwtSecurityToken
            (
                issuer: _issuer,
                audience: _audience,
                expires: DateTime.UtcNow.AddMinutes(10),
                claims: authClaims,
                signingCredentials: new SigningCredentials(_authSigningKey, SecurityAlgorithms.HmacSha256)

            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string CreateRefreshToken( String userId)
        {
            var authClaims = new List<Claim>()
            {
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Sub, userId)
            };

            var token = new JwtSecurityToken
            (
                issuer: _issuer,
                audience: _audience,
                expires: DateTime.UtcNow.AddHours(48),
                claims: authClaims,
                signingCredentials: new SigningCredentials(_authSigningKey, SecurityAlgorithms.HmacSha256)

            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public bool TryParseRefreshToken(string refreshToken, out ClaimsPrincipal? claims)
        {
            claims = null;
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidIssuer = _issuer,
                    ValidAudience = _audience,
                    IssuerSigningKey = _authSigningKey,
                    ValidateLifetime = true
                };
                claims = tokenHandler.ValidateToken(refreshToken, validationParameters, out _);
                return true;
            }
            catch 
            { 
                return false;
            }
        }
    }
}
