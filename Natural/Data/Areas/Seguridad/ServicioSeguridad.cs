using Microsoft.IdentityModel.Tokens;
using Natural.Common;
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

        public (string Token, DateTime ExpiraEn) GenerarTokenJwt(PersonaViewModel persona, string? rol, int? usuarioId = null)
        {
            var expiresMinutes = int.TryParse(_configuration["Jwt:ExpiresMinutes"], out var minutes)
                ? minutes
                : 720;

            return EmitirToken(persona.Id, persona.Nombre, persona.EmpresaId ?? 0, rol, usuarioId, expiresMinutes, impersonadoPor: null);
        }

        /// <summary>
        /// Token de soporte: entra a una empresa como su administrador (rol 01). Dura poco y guarda quién lo pidió.
        /// </summary>
        public (string Token, DateTime ExpiraEn) GenerarTokenImpersonacion(
            int personaId, string nombre, int empresaId, int usuarioId, string actor, int minutos)
        {
            var duracion = Math.Clamp(minutos, 5, 120);
            return EmitirToken(personaId, nombre, empresaId, Roles.Administrador, usuarioId, duracion, impersonadoPor: actor);
        }

        private (string Token, DateTime ExpiraEn) EmitirToken(
            int personaId, string nombre, int empresaId, string? rol, int? usuarioId, int expiresMinutes, string? impersonadoPor)
        {
            var issuer = _configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException("Jwt:Issuer no está configurado.");
            var audience = _configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException("Jwt:Audience no está configurado.");
            var key = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("Jwt:Key no está configurado.");

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, personaId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(Roles.ClaimEmpresaId, empresaId.ToString()),
                new Claim(ClaimTypes.Name, nombre)
            };

            if (usuarioId is int idUsuario)
                claims.Add(new Claim(Roles.ClaimUsuarioId, idUsuario.ToString()));

            if (!string.IsNullOrWhiteSpace(impersonadoPor))
                claims.Add(new Claim(Roles.ClaimImpersonadoPor, impersonadoPor));

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
