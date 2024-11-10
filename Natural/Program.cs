using Microsoft.EntityFrameworkCore;
using Natural.Data;
using Natural.Data.Areas.Estructura;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Registrar el DbContext con la cadena de conexión
builder.Services.AddDbContext<NaturalDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ServicioEstructura>();

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

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
