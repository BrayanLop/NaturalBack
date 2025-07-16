using Microsoft.EntityFrameworkCore;
using Natural.Data.Areas.Configuracion;
using Natural.Data.Areas.Contabilidad;
using Natural.Data.Areas.Estructura;
using Natural.Data.Areas.Gestion;
using Natural.Data.Areas.Sesion;
using Natural.Data.Domain;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Registrar el DbContext con la cadena de conexión
builder.Services.AddDbContext<NaturalDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ServicioEstructura>();

builder.Services.AddScoped<ServicioGestion>();

builder.Services.AddScoped<ServicioLogin>();

builder.Services.AddScoped<ServicioConfiguracion>();

builder.Services.AddScoped<ServicioContabilidad>();

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Habilita CORS
app.UseCors("AllowAll");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
