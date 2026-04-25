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
            "No se pudieron aplicar migraciones. Compruebe la cadena de conexión, SSL, firewall de MySQL en Azure y que el servidor permita conexiones desde App Service.");
        throw;
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

    return configuration["ConnectionStrings:DefaultConnection"];
}
