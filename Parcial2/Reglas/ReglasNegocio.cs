namespace Parcial2.Reglas;

public static class ReglasNegocio
{
    public static readonly string[] CarnetsAutorizados =
    {
        "MED-1010", "MED-2020", "MED-3030", "MED-4040", "MED-5050"
    };

    public const string EstadoEnEspera = "En espera";
    public const string EstadoAtendido = "Atendido";
    public const string EstadoDerivado = "Derivado";

    public static readonly string[] EstadosValidos =
    {
        EstadoEnEspera, EstadoAtendido, EstadoDerivado
    };

    public const int GravedadMaxima = 5;
    public const int GravedadMinima = 1;
    public const int MaximoCriticosEnEspera = 5;

    public const string PrefijoId = "PAC-2026-";
    public const string MensajeCapacidadCritica =
        "Capacidad máxima alcanzada. Redirección inmediata a otro hospital sugerida.";

    public static bool EsCarnetAutorizado(string? carnet)
    {
        if (string.IsNullOrWhiteSpace(carnet)) return false;
        foreach (var c in CarnetsAutorizados)
        {
            if (c == carnet) return true;
        }
        return false;
    }

    public static bool EsEstadoValido(string estado)
    {
        foreach (var e in EstadosValidos)
        {
            if (e == estado) return true;
        }
        return false;
    }
}
