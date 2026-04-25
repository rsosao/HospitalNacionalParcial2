using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Parcial2.Data;
using Parcial2.Services;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// App Service / IIS detrás de proxy: HTTPS y host correctos (evita URLs http o rotas mal resueltas en Scalar).
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

builder.Services.AddOpenApi();
builder.Services.AddControllers();

var connectionString = ResolverCadenaConexion(builder.Configuration);
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "Defina la base de datos: en Azure use 'Connection strings' (nombre DefaultConnection, tipo MySQL o Custom) " +
        "o un App setting 'ConnectionStrings__DefaultConnection' con la cadena completa.");

builder.Services.AddDbContext<HospitalContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0))));

builder.Services.AddScoped<GeneradorIdPaciente>();

var app = builder.Build();

app.UseForwardedHeaders();

// Migraciones: si falla (conexión, SSL, firewall), el proceso sigue vivo para /health, Scalar y diagnóstico.
// Revisa Log stream: el error real de MySQL queda registrado aquí.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HospitalContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    try
    {
        await db.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex,
            "No se pudieron aplicar migraciones. Revise: cadena de conexión, SslMode, firewall de Azure MySQL (Allow public access / reglas) y que 'DefaultConnection' exista en el App Service. La API devolverá error al usar la base hasta que esto se corrija.");
    }
}

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    // Documento v1 de MapOpenApi() → /openapi/v1.json. Base dinámica para Azure (mismo host que la petición).
    options
        .WithTitle("Parcial2 — API Hospital")
        .WithOpenApiRoutePattern("/openapi/v1.json")
        .WithDynamicBaseServerUrl(true);
});
// Scalar 2: documento explícito en ruta; /scalar a veces redirige a /scalar/v1
app.MapGet("/", () => Results.Redirect("/scalar/v1"));

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

static string? ResolverCadenaConexion(ConfigurationManager configuration)
{
    var cs = configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrWhiteSpace(cs))
        return cs;

    // App Service: si la cadena está en "Connection strings" con tipo MySQL/Custom
    foreach (var key in new[]
             {
                 "MYSQLCONNSTR_DefaultConnection",
                 "CUSTOMCONNSTR_DefaultConnection",
                 "SQLCONNSTR_DefaultConnection",
                 "MYSQLCONNSTR_defaultconnection",
                 "CUSTOMCONNSTR_defaultconnection"
             })
    {
        var v = configuration[key];
        if (!string.IsNullOrWhiteSpace(v))
            return v;
    }

    return configuration["ConnectionStrings:DefaultConnection"]
        ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
        ?? Environment.GetEnvironmentVariable("MYSQLCONNSTR_defaultconnection")
        ?? Environment.GetEnvironmentVariable("CUSTOMCONNSTR_defaultconnection");
}
