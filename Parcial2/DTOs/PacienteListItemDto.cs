using System.Text.Json.Serialization;

namespace Parcial2.DTOs;

public class PacienteListItemDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("gravedad")]
    public int Gravedad { get; set; }

    [JsonPropertyName("estado")]
    public string Estado { get; set; } = string.Empty;

    [JsonPropertyName("carnetMedico")]
    public string CarnetMedico { get; set; } = string.Empty;

    [JsonPropertyName("fechaRegistro")]
    public DateTime FechaRegistro { get; set; }
}
