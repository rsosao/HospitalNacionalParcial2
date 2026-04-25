using System.Text.Json.Serialization;

namespace Parcial2.DTOs;

public class UpdatePacienteRequest
{
    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("gravedad")]
    public int Gravedad { get; set; }

    [JsonPropertyName("estado")]
    public string Estado { get; set; } = string.Empty;

    [JsonPropertyName("carnetMedico")]
    public string CarnetMedico { get; set; } = string.Empty;
}
