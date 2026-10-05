using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Natural.Common
{
    /// <summary>
    /// Base de los controllers autenticados. La empresa y la persona se toman SIEMPRE del JWT
    /// (no de headers enviados por el cliente) para que un usuario no pueda operar sobre otra empresa.
    /// </summary>
    [ApiController]
    public abstract class ApiControllerBase : ControllerBase
    {
        protected bool TryObtenerEmpresaId(out int empresaId, out IActionResult? errorResult)
        {
            if (TryObtenerClaimInt(Roles.ClaimEmpresaId, out empresaId) && empresaId > 0)
            {
                errorResult = null;
                return true;
            }

            errorResult = Forbid();
            return false;
        }

        protected bool TryObtenerPersonaId(out int personaId, out IActionResult? errorResult)
        {
            if (TryObtenerClaimInt(ClaimTypes.NameIdentifier, out personaId)
                || TryObtenerClaimInt(JwtRegisteredClaimNames.Sub, out personaId))
            {
                errorResult = null;
                return true;
            }

            errorResult = Forbid();
            return false;
        }

        protected bool EsAdministrador => User.IsInRole(Roles.Administrador);

        protected bool PuedeGestionar => User.IsInRole(Roles.Administrador) || User.IsInRole(Roles.SuperEmpleado);

        /// <summary>Un trabajador solo puede consultar su propia información; admin y super empleado, la de cualquiera.</summary>
        protected bool PuedeConsultarPersona(int personaId) =>
            PuedeGestionar || (TryObtenerPersonaId(out var personaSesion, out _) && personaSesion == personaId);

        private bool TryObtenerClaimInt(string claimType, out int valor)
        {
            valor = default;
            var claim = User.FindFirst(claimType)?.Value;
            return int.TryParse(claim, out valor);
        }
    }
}
