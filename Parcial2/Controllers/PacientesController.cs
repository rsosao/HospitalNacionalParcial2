using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Parcial2.Data;
using Parcial2.DTOs;
using Parcial2.Models;
using Parcial2.Reglas;
using Parcial2.Services;

namespace Parcial2.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PacientesController : ControllerBase
{
    private static readonly SemaphoreSlim AltaLock = new(1, 1);

    private readonly HospitalContext _db;
    private readonly GeneradorIdPaciente _generadorId;

    public PacientesController(HospitalContext db, GeneradorIdPaciente generadorId)
    {
        _db = db;
        _generadorId = generadorId;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PacienteListItemDto>>> Get(CancellationToken ct)
    {
        var lista = await _db.Pacientes.AsNoTracking().ToListAsync(ct);
        OrdenadorPacientes.OrdenarDescGravedadAscFecha(lista);
        return Ok(lista.Select(Map).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PacienteListItemDto>> GetById(
        [FromRoute] string id,
        CancellationToken ct)
    {
        var p = await _db.Pacientes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return NotFound();
        return Ok(Map(p));
    }

    [HttpPost]
    public async Task<ActionResult<PacienteListItemDto>> Post(
        [FromBody] CreatePacienteRequest request,
        CancellationToken ct)
    {
        if (!ReglasNegocio.EsCarnetAutorizado(request.CarnetMedico))
            return Unauthorized();

        var validoEntrada = ValidarModelo(
            request.Nombre,
            request.Gravedad,
            request.Estado);
        if (validoEntrada is { } err)
            return err;

        await AltaLock.WaitAsync(ct);
        try
        {
            if (await EsCasoCriticoLlenoAsync(request.Gravedad, request.Estado, excluirId: null, ct))
                return BadRequest(new { message = ReglasNegocio.MensajeCapacidadCritica });

            var id = await _generadorId.SiguienteIdAsync(ct);
            var ahora = DateTime.UtcNow;
            var p = new Paciente
            {
                Id = id,
                Nombre = request.Nombre.Trim(),
                Gravedad = request.Gravedad,
                Estado = request.Estado,
                CarnetMedico = request.CarnetMedico.Trim(),
                FechaRegistro = ahora
            };
            _db.Pacientes.Add(p);
            await _db.SaveChangesAsync(ct);
            return CreatedAtAction(nameof(GetById), new { id = p.Id }, Map(p));
        }
        finally
        {
            AltaLock.Release();
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PacienteListItemDto>> Put(
        [FromRoute] string id,
        [FromBody] UpdatePacienteRequest request,
        CancellationToken ct)
    {
        if (!ReglasNegocio.EsCarnetAutorizado(request.CarnetMedico))
            return Unauthorized();

        var entidad = await _db.Pacientes.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entidad is null) return NotFound();

        var validoEntrada = ValidarModelo(
            request.Nombre,
            request.Gravedad,
            request.Estado);
        if (validoEntrada is { } err)
            return err;

        if (await EsCasoCriticoLlenoAsync(
                request.Gravedad,
                request.Estado,
                excluirId: id,
                ct))
            return BadRequest(new { message = ReglasNegocio.MensajeCapacidadCritica });

        entidad.Nombre = request.Nombre.Trim();
        entidad.Gravedad = request.Gravedad;
        entidad.Estado = request.Estado;
        entidad.CarnetMedico = request.CarnetMedico.Trim();
        await _db.SaveChangesAsync(ct);
        return Ok(Map(entidad));
    }

    private async Task<bool> EsCasoCriticoLlenoAsync(
        int gravedad,
        string estado,
        string? excluirId,
        CancellationToken ct)
    {
        if (gravedad != ReglasNegocio.GravedadMaxima) return false;
        if (estado != ReglasNegocio.EstadoEnEspera) return false;

        var q = _db.Pacientes.AsNoTracking()
            .Where(p => p.Gravedad == ReglasNegocio.GravedadMaxima
                && p.Estado == ReglasNegocio.EstadoEnEspera);
        if (excluirId is not null)
            q = q.Where(p => p.Id != excluirId);
        var c = await q.CountAsync(ct);
        return c >= ReglasNegocio.MaximoCriticosEnEspera;
    }

    private static ActionResult? ValidarModelo(string nombre, int gravedad, string estado)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return new BadRequestObjectResult("El nombre es obligatorio.");
        if (gravedad < ReglasNegocio.GravedadMinima || gravedad > ReglasNegocio.GravedadMaxima)
            return new BadRequestObjectResult("La gravedad debe estar entre 1 y 5.");
        if (!ReglasNegocio.EsEstadoValido(estado))
            return new BadRequestObjectResult("Estado no válido. Use: En espera, Atendido o Derivado.");
        return null;
    }

    private static PacienteListItemDto Map(Paciente p) => new()
    {
        Id = p.Id,
        Nombre = p.Nombre,
        Gravedad = p.Gravedad,
        Estado = p.Estado,
        CarnetMedico = p.CarnetMedico,
        FechaRegistro = p.FechaRegistro
    };
}
