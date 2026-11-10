
using Garupa.Application.Interfaces;
using Garupa.Application.Services;
using Garupa.Domain.Repositories;
using Garupa.Infrastructure.Data;
using Garupa.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Garupa.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddJsonConsole();

// Controllers e documentação
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Garupa SP API",
        Version = "v1",
        Description = "API de cadastro, risco viário e socorro. Exemplos com dados pessoais fictícios. Datas em UTC."
    });
    options.OperationFilter<Garupa.Api.Documentacao.ContratoSwagger>();
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<Garupa.Api.Errors.ApiExceptionHandler>();
// Evita registrar excecoes e URLs que possam conter dados pessoais ou tokens.
builder.Logging.AddFilter("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware", LogLevel.None);
builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore.Routing.EndpointMiddleware", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.None);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Update", LogLevel.None);
if (OperatingSystem.IsWindows())
{
    builder.Logging.AddFilter<Microsoft.Extensions.Logging.EventLog.EventLogLoggerProvider>(
        "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware", LogLevel.None);
    builder.Logging.AddFilter<Microsoft.Extensions.Logging.EventLog.EventLogLoggerProvider>(
        "Microsoft.EntityFrameworkCore.Database.Command", LogLevel.None);
    builder.Logging.AddFilter<Microsoft.Extensions.Logging.EventLog.EventLogLoggerProvider>(
        "Microsoft.EntityFrameworkCore.Update", LogLevel.None);
}
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context => new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(
        new Microsoft.AspNetCore.Mvc.ProblemDetails { Status = 400, Title = "Dados inválidos." });
});
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ISosRepository, SosRepository>();
builder.Services.AddScoped<ISosService, SosService>();
builder.Services.AddScoped<IPrivacidadeRepository, PrivacidadeRepository>();
builder.Services.AddScoped<IPrivacidadeService, PrivacidadeService>();
builder.Services.AddScoped<IPerfilRepository, PerfilRepository>();
builder.Services.AddScoped<IPerfilService, PerfilService>();
builder.Services.AddScoped<IConteudoRepository, ConteudoRepository>();
builder.Services.AddScoped<IConteudoService, ConteudoService>();
builder.Services.AddScoped<IVestivelRepository, VestivelRepository>();
builder.Services.AddScoped<IVestivelService, VestivelService>();
builder.Services.AddScoped<IRelatoRepository, RelatoRepository>();
builder.Services.AddScoped<IRelatoService, RelatoService>();
builder.Services.AddScoped<IManutencaoRepository, ManutencaoRepository>();
if (!args.Contains("preparar-banco") && !args.Contains("carregar-risco"))
    builder.Services.AddHostedService<Garupa.Api.Services.ManutencaoBackgroundService>();
builder.Services.AddSingleton<ILimiteMunicipal, LimiteSaoPaulo>();
builder.Services.AddSingleton<ICriptografiaDados>(_ =>
{
    var root = builder.Environment.ContentRootPath;
    if (!File.Exists(Path.Combine(root, "Garupa API.sln"))) root = Path.GetFullPath(Path.Combine(root, ".."));
    var arquivo = builder.Configuration["Criptografia:ArquivoChave"] ?? Path.Combine(root, ".local", "keys", "ficha-medica.key");
    return CriptografiaDados.Carregar(builder.Configuration["Criptografia:Chave"], arquivo, builder.Environment.IsDevelopment());
});

// Banco de dados PostgreSQL
builder.Services.AddDbContext<GarupaDbContext>((services, options) =>
{
    var connectionString = services.GetRequiredService<IConfiguration>()
        .GetConnectionString("GarupaDb")
        ?? throw new InvalidOperationException("Connection string GarupaDb não configurada.");
    options.UseNpgsql(connectionString);
});

// Injeção de dependência
builder.Services.AddScoped<
    IUsuarioRepository,
    UsuarioRepository>();

builder.Services.AddScoped<
    IUsuarioService,
    UsuarioService>();

var app = builder.Build();

if (args.Length > 0 && args[0] is "preparar-banco" or "carregar-risco")
{
    await using var scope = app.Services.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<GarupaDbContext>();
    if (args[0] == "preparar-banco")
    {
        await PreparacaoBanco.ExecutarAsync(context, scope.ServiceProvider.GetRequiredService<ICriptografiaDados>());
        Console.WriteLine("Migrations aplicadas e fichas protegidas.");
    }
    else
    {
        if (args.Length != 2) throw new ArgumentException("Informe o caminho do CSV.");
        var quantidade = await new ImportadorRisco(context).CarregarAsync(args[1]);
        Console.WriteLine($"Carga concluída: {quantidade} células.");
    }
    return;
}

// Tratamento global de exceções
app.UseRouting();
app.Use(async (context, next) =>
{
    var inicio = System.Diagnostics.Stopwatch.GetTimestamp();
    context.Response.Headers["X-Correlation-ID"] = context.TraceIdentifier;
    try
    {
        await next(context);
    }
    finally
    {
        var rota = (context.GetEndpoint() as Microsoft.AspNetCore.Routing.RouteEndpoint)?.RoutePattern.RawText ?? "(sem rota)";
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Garupa.Api.Requisicoes");
        logger.LogInformation("HTTP {Metodo} {Rota} {Status} {DuracaoMs}ms {Correlacao}",
            context.Request.Method, rota, context.Response.StatusCode,
            System.Diagnostics.Stopwatch.GetElapsedTime(inicio).TotalMilliseconds, context.TraceIdentifier);
    }
});
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
