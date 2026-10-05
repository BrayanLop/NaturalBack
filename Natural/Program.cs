using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Natural.Common;
using Natural.Data.Areas.Configuracion;
using Natural.Data.Areas.Contabilidad;
using Natural.Data.Areas.Estructura;
using Natural.Data.Areas.Gestion;
using Natural.Data.Areas.Seguridad;
using Natural.Data.Areas.Sesion;
using Natural.Data.Domain;
using Natural.Data.Domain.Sesion;
using Natural.Data.Services;
using System.Text;
using Supabase;

var builder = WebApplication.CreateBuilder(args);

// Los secretos NO van en appsettings.json: en local se usan user-secrets y en el servidor variables de entorno
// (ConnectionStrings__DefaultConnection, Supabase__Url, Supabase__Key, Jwt__Key, ...).
string ConfigRequerida(string clave) =>
    builder.Configuration[clave] is { Length: > 0 } valor
        ? valor
        : throw new InvalidOperationException($"Falta la configuración '{clave}'. Defínela con user-secrets o variables de entorno.");

var connectionString = ConfigRequerida("ConnectionStrings:DefaultConnection");
var supabaseUrl = ConfigRequerida("Supabase:Url");
var supabaseKey = ConfigRequerida("Supabase:Key");
var jwtKey = ConfigRequerida("Jwt:Key");

builder.Services.AddDbContext<NaturalDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddScoped<Supabase.Client>(_ => new Supabase.Client(supabaseUrl, supabaseKey, new SupabaseOptions
{
    AutoRefreshToken = true,
    AutoConnectRealtime = false
}));

builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddScoped<SupabaseStorageService>();
builder.Services.AddScoped<ServicioEstructura>();
builder.Services.AddScoped<ServicioGestion>();
builder.Services.AddScoped<ServicioEvidencia>();
builder.Services.AddScoped<ServicioLogin>();
builder.Services.AddScoped<ServicioConfiguracion>();
builder.Services.AddScoped<ServicioContabilidad>();
builder.Services.AddScoped<ServicioSeguridad>();

// CORS: si no se configuran orígenes (Cors:AllowedOrigins) se permite cualquiera, como antes.
var origenesPermitidos = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (origenesPermitidos.Length > 0)
            policy.WithOrigins(origenesPermitidos);
        else
            policy.AllowAnyOrigin();

        policy.AllowAnyMethod().AllowAnyHeader();
    });
});

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
       .AddJwtBearer(options =>
       {
           options.TokenValidationParameters = new TokenValidationParameters
           {
               ValidateIssuer = true,
               ValidateAudience = true,
               ValidateLifetime = true,
               ValidateIssuerSigningKey = true,
               ValidIssuer = builder.Configuration["Jwt:Issuer"],
               ValidAudience = builder.Configuration["Jwt:Audience"],
               IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
           };
       });

builder.Services.AddAuthorization(options =>
{
    // Todo endpoint exige usuario autenticado salvo que tenga [AllowAnonymous].
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    options.AddPolicy(Roles.PoliticaGestion, policy => policy.RequireRole(Roles.Administrador, Roles.SuperEmpleado));
});

var app = builder.Build();

// Errores no controlados: se registran en el log y el cliente recibe un ProblemDetails 500 sin detalles internos.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
