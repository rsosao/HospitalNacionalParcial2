using Parcial2.Models;

namespace Parcial2.Services;

/// <summary>Ordenación en memoria (bubble sort): gravedad 5→1, desempate por registro más antiguo. No se usa SQL ni métodos de orden de LINQ.</summary>
public static class OrdenadorPacientes
{
    public static void OrdenarDescGravedadAscFecha(List<Paciente> lista)
    {
        if (lista.Count < 2) return;
        for (var i = 0; i < lista.Count - 1; i++)
        {
            for (var j = 0; j < lista.Count - 1 - i; j++)
            {
                if (DebeIntercambiar(lista[j], lista[j + 1]))
                {
                    (lista[j], lista[j + 1]) = (lista[j + 1], lista[j]);
                }
            }
        }
    }

    private static bool DebeIntercambiar(Paciente a, Paciente b)
    {
        if (a.Gravedad < b.Gravedad) return true;
        if (a.Gravedad > b.Gravedad) return false;
        return a.FechaRegistro > b.FechaRegistro;
    }
}
