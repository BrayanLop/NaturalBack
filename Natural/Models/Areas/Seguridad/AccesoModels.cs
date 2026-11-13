namespace Natural.Models.Areas.Seguridad
{
    /// <summary>Una persona de la empresa y, si lo tiene, su acceso (usuario) a la aplicación.</summary>
    public class AccesoViewModel
    {
        public int PersonaId { get; set; }
        public string Nombre { get; set; } = "";
        public string Apellido { get; set; } = "";
        public string? Email { get; set; }
        public int? UsuarioId { get; set; }
        public string? NombreUsuario { get; set; }
        public bool TieneAcceso { get; set; }
        public bool Activo { get; set; }
        public string? Rol { get; set; }
    }

    public class CrearAccesoRequest
    {
        public int PersonaId { get; set; }
        public string? NombreUsuario { get; set; }
        /// <summary>SHA-256 en base64 (44 caracteres), igual que el login y el cambio de contraseña.</summary>
        public string? Contrasena { get; set; }
        public string? Rol { get; set; }
    }

    public class CambiarRolRequest
    {
        public string? Rol { get; set; }
    }

    public class RestablecerContrasenaRequest
    {
        /// <summary>SHA-256 en base64 (44 caracteres).</summary>
        public string? NuevaContrasena { get; set; }
    }

    public class EmpresaPlataformaViewModel
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public string? Nit { get; set; }
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public bool Activa { get; set; }
        public int TotalPersonas { get; set; }
        public int TotalAccesos { get; set; }
        public int AccesosActivos { get; set; }
    }

    public class EmpresaRequest
    {
        public string? Nombre { get; set; }
        public string? Nit { get; set; }
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
    }

    public class CrearEmpresaRequest : EmpresaRequest
    {
        /// <summary>Primer administrador (rol 01) de la empresa.</summary>
        public AdminEmpresaRequest? Admin { get; set; }
    }

    public class AdminEmpresaRequest
    {
        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
        public string? Cedula { get; set; }
        public string? Email { get; set; }
        public string? Celular { get; set; }
        public string? NombreUsuario { get; set; }
        /// <summary>SHA-256 en base64 (44 caracteres).</summary>
        public string? Contrasena { get; set; }
    }

    public class ImpersonarRequest
    {
        /// <summary>Identificador del superadmin que pide el acceso (queda en el token y en el log).</summary>
        public string? Actor { get; set; }
        public int Minutos { get; set; } = 30;
    }
}
