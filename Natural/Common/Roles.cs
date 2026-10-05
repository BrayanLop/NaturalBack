namespace Natural.Common
{
    public static class Roles
    {
        public const string Administrador = "01";
        public const string Trabajador = "02";
        public const string SuperEmpleado = "03";

        /// <summary>Política para acciones de gestión (admin y super empleado, igual que en el front).</summary>
        public const string PoliticaGestion = "Gestion";

        public const string ClaimEmpresaId = "empresaId";
    }
}
