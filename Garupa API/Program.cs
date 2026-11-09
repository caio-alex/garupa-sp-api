
using Garupa.Application.Interfaces;
using Garupa.Application.Services;
using Garupa.Domain.Repositories;
using Garupa.Infrastructure.Data;
using Garupa.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Controllers e documentação
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

// Banco de dados PostgreSQL
var connectionString = builder.Configuration
    .GetConnectionString("GarupaDb")
    ?? throw new InvalidOperationException(
        "Connection string GarupaDb não configurada.");

builder.Services.AddDbContext<GarupaDbContext>(options =>
    options.UseNpgsql(connectionString));

// Injeção de dependência
builder.Services.AddScoped<
    IUsuarioRepository,
    UsuarioRepository>();

builder.Services.AddScoped<
    IUsuarioService,
    UsuarioService>();

var app = builder.Build();

// Tratamento global de exceções
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Necessário futuramente para os testes de integração
public partial class Program { }
