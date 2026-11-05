using Microsoft.AspNetCore.Mvc;

namespace Natural.Common
{
    public enum TipoError
    {
        Ninguno,
        NoEncontrado,
        Invalido,
        Conflicto
    }

    /// <summary>Resultado de una operación de negocio: valor o error con mensaje para el usuario.</summary>
    public sealed class Resultado<T>
    {
        public T? Valor { get; }
        public TipoError Error { get; }
        public string? Mensaje { get; }
        public bool Ok => Error == TipoError.Ninguno;

        private Resultado(T? valor, TipoError error, string? mensaje)
        {
            Valor = valor;
            Error = error;
            Mensaje = mensaje;
        }

        public static Resultado<T> Exito(T valor) => new(valor, TipoError.Ninguno, null);
        public static Resultado<T> NoEncontrado(string mensaje) => new(default, TipoError.NoEncontrado, mensaje);
        public static Resultado<T> Invalido(string mensaje) => new(default, TipoError.Invalido, mensaje);
        public static Resultado<T> Conflicto(string mensaje) => new(default, TipoError.Conflicto, mensaje);
    }

    public static class ResultadoExtensions
    {
        /// <summary>Convierte el resultado en respuesta HTTP: errores como texto simple (convención del API).</summary>
        public static IActionResult ToActionResult<T>(this ControllerBase controller, Resultado<T> resultado, Func<T, IActionResult>? exito = null)
        {
            if (resultado.Ok)
                return exito is null ? controller.Ok(resultado.Valor) : exito(resultado.Valor!);

            return resultado.Error switch
            {
                TipoError.NoEncontrado => controller.NotFound(resultado.Mensaje),
                TipoError.Conflicto => controller.Conflict(resultado.Mensaje),
                _ => controller.BadRequest(resultado.Mensaje)
            };
        }
    }
}
