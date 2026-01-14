using Microsoft.IdentityModel.Tokens;
using Natural.Models.Areas.Estructura;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Natural.Data.Areas.Seguridad
{
    public class ServicioSeguridad
    {
        private readonly IConfiguration _configuration;

        public ServicioSeguridad(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public (string Token, DateTime ExpiraEn) GenerarTokenJwt(PersonaViewModel persona, string? rol)
        {
            var issuer = _configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException("Jwt:Issuer no está configurado.");
            var audience = _configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException("Jwt:Audience no está configurado.");
            var key = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("Jwt:Key no está configurado.");

            var expiresMinutes = int.TryParse(_configuration["Jwt:ExpiresMinutes"], out var minutes)
                ? minutes
                : 720;

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, persona.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("empresaId", (persona.EmpresaId ?? 0).ToString()),
                new Claim(ClaimTypes.Name, persona.Nombre)
            };

            if (!string.IsNullOrWhiteSpace(rol))
            {
                claims.Add(new Claim(ClaimTypes.Role, rol));
            }

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            var expiraEn = DateTime.UtcNow.AddMinutes(expiresMinutes);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiraEn,
                signingCredentials: credentials);

            return (new JwtSecurityTokenHandler().WriteToken(token), expiraEn);
        }

    }
}
