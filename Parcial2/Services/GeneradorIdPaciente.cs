using Microsoft.EntityFrameworkCore;
using Parcial2.Data;
using Parcial2.Reglas;

namespace Parcial2.Services;

/// <summary>Genera PAC-2026-XXX en aplicación, sin autoincrement SQL en el id lógico.</summary>
public class GeneradorIdPaciente
{
    private readonly HospitalContext _db;

    public GeneradorIdPaciente(HospitalContext db) => _db = db;

    public async Task<string> SiguienteIdAsync(CancellationToken ct = default)
    {
        var ids = await _db.Pacientes.AsNoTracking().Select(p => p.Id).ToListAsync(ct);
        int max = 0;
        foreach (var id in ids)
        {
            var n = ParsearSecuencia(id);
            if (n > max) max = n;
        }

        return $"{ReglasNegocio.PrefijoId}{max + 1:000}";
    }

    private static int ParsearSecuencia(string id)
    {
        if (string.IsNullOrEmpty(id) || !id.StartsWith(ReglasNegocio.PrefijoId, StringComparison.Ordinal)) return 0;
        var suffix = id[ReglasNegocio.PrefijoId.Length..];
        return int.TryParse(suffix, out var n) ? n : 0;
    }
}
