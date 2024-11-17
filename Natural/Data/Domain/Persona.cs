namespace Natural.Data.Domain
{
    public class Persona
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Cedula { get; set; }
        public string Residencia { get; set; }
        public string Email { get; set; }
        public int Edad { get; set; }
        public string Celular { get; set; }
        public DateTime FechaNacimiento { get; set; }
    }
}
