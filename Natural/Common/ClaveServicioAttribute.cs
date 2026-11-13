using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Natural.Common
{
    /// <summary>
    /// Protege endpoints que solo debe llamar otro servicio de confianza (el microservicio de plataforma).
    /// Exige el header <c>X-Service-Key</c> igual a la configuración <c>Plataforma:ClaveServicio</c>.
    /// Si la clave no está configurada (o es corta) el endpoint queda deshabilitado y responde 404.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class ClaveServicioAttribute : Attribute, IAuthorizationFilter
    {
        public const string Header = "X-Service-Key";
        private const int LongitudMinima = 32;

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var esperada = context.HttpContext.RequestServices
                .GetRequiredService<IConfiguration>()["Plataforma:ClaveServicio"];

            if (string.IsNullOrEmpty(esperada) || esperada.Length < LongitudMinima)
            {
                context.Result = new NotFoundResult();
                return;
            }

            var recibida = context.HttpContext.Request.Headers[Header].ToString();
            if (!SonIguales(esperada, recibida))
                context.Result = new UnauthorizedObjectResult("Clave de servicio inválida.");
        }

        // Se comparan los hashes para que la comparación sea de tiempo constante sin importar la longitud.
        private static bool SonIguales(string a, string b) =>
            CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(Encoding.UTF8.GetBytes(a)),
                SHA256.HashData(Encoding.UTF8.GetBytes(b)));
    }
}
