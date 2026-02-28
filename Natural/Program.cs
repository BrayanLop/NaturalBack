using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Natural.Data.Areas.Configuracion;
using Natural.Data.Areas.Contabilidad;
using Natural.Data.Areas.Estructura;
using Natural.Data.Areas.Gestion;
using Natural.Data.Areas.Seguridad;
using Natural.Data.Areas.Sesion;
using Natural.Data.Domain;
using Natural.Data.Services;
using System.Text;
using Supabase;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Registrar el DbContext con la cadena de conexión
builder.Services.AddDbContext<NaturalDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Registrar cliente de Supabase
var supabaseUrl = builder.Configuration["Supabase:Url"] ?? throw new Exception("Supabase URL no configurada");
var supabaseKey = builder.Configuration["Supabase:Key"] ?? throw new Exception("Supabase Key no configurada");

builder.Services.AddScoped<Supabase.Client>(_ => new Supabase.Client(supabaseUrl, supabaseKey, new SupabaseOptions
{
    AutoRefreshToken = true,
    AutoConnectRealtime = false
}));

builder.Services.AddScoped<SupabaseStorageService>();
builder.Services.AddScoped<ServicioEstructura>();
builder.Services.AddScoped<ServicioGestion>();
builder.Services.AddScoped<ServicioEvidencia>();
builder.Services.AddScoped<ServicioLogin>();
builder.Services.AddScoped<ServicioConfiguracion>();
builder.Services.AddScoped<ServicioContabilidad>();
builder.Services.AddScoped<ServicioSeguridad>();


// Habilita CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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
               IssuerSigningKey = new SymmetricSecurityKey(
                   Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
           };
       });

   builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
    app.UseSwagger();
    app.UseSwaggerUI();

// Habilita CORS
app.UseCors("AllowAll");

//app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
