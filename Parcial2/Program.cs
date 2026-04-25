using Microsoft.EntityFrameworkCore;
using Parcial2.Data;
using Parcial2.Services;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

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
app.MapScalarApiReference();
app.MapGet("/", () => Results.Redirect("/scalar"));

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
