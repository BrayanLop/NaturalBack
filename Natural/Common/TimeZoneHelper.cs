namespace Natural.Common
{
    public static class TimeZoneHelper
    {
        // Colombia no tiene horario de verano: si el sistema no tiene la zona (p.ej. contenedor sin tzdata)
        // se usa un desfase fijo de UTC-5.
        private static readonly TimeZoneInfo ZonaColombia = ObtenerZonaColombia();

        private static TimeZoneInfo ObtenerZonaColombia()
        {
            foreach (var id in new[] { "America/Bogota", "SA Pacific Standard Time" })
            {
                if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zona))
                    return zona;
            }

            return TimeZoneInfo.CreateCustomTimeZone("Colombia", TimeSpan.FromHours(-5), "Colombia", "Colombia");
        }

        public static DateTime ColombiaNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ZonaColombia);

        public static DateTime ColombiaNowUtc => ConvertColombiaToUtc(ColombiaNow);

        public static DateTime ConvertColombiaToUtc(DateTime fechaColombia)
        {
            var local = DateTime.SpecifyKind(fechaColombia, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(local, ZonaColombia);
        }

        public static DateTime? ConvertColombiaToUtc(DateTime? fechaColombia) =>
            fechaColombia.HasValue ? ConvertColombiaToUtc(fechaColombia.Value) : null;

        public static DateTime ConvertUtcToColombia(DateTime fechaUtc)
        {
            var utc = DateTime.SpecifyKind(fechaUtc, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(utc, ZonaColombia);
        }

        public static (DateTime InicioUtc, DateTime FinUtc) RangoDiaActualUtc()
        {
            var hoyColombia = ColombiaNow.Date;
            var inicioUtc = ConvertColombiaToUtc(hoyColombia);
            var finUtc = ConvertColombiaToUtc(hoyColombia.AddDays(1));
            return (inicioUtc, finUtc);
        }

        public static (DateTime InicioUtc, DateTime FinUtc) RangoUtc(DateTime fechaDesdeColombia, DateTime fechaHastaColombia)
        {
            var inicioUtc = ConvertColombiaToUtc(fechaDesdeColombia.Date);
            var finUtc = ConvertColombiaToUtc(fechaHastaColombia.Date.AddDays(1));
            return (inicioUtc, finUtc);
        }
    }
}
