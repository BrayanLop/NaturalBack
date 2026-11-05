namespace Natural.Common
{
    public static class Roles
    {
        /// <summary>Rol de plataforma (superadmin de la solución): no pertenece a ninguna empresa.</summary>
        public const string Plataforma = "00";
        public const string Administrador = "01";
        public const string Trabajador = "02";
        public const string SuperEmpleado = "03";

        /// <summary>Roles que un administrador de empresa puede asignar a sus usuarios.</summary>
        public static readonly string[] AsignablesEnEmpresa = [Administrador, Trabajador, SuperEmpleado];

        /// <summary>Política para acciones de gestión (admin y super empleado, igual que en el front).</summary>
        public const string PoliticaGestion = "Gestion";

        /// <summary>Política solo para el administrador de la empresa (rol 01).</summary>
        public const string PoliticaAdministrador = "Administrador";

        public const string ClaimEmpresaId = "empresaId";

        /// <summary>Id del Usuario (no de la Persona): permite invalidar el token si el usuario se desactiva.</summary>
        public const string ClaimUsuarioId = "usuarioId";

        /// <summary>Presente solo en tokens de soporte emitidos por la plataforma; guarda quién los pidió.</summary>
        public const string ClaimImpersonadoPor = "impersonadoPor";
    }
}
