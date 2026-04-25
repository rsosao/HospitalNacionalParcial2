using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Parcial2.Models;

/// <summary>Entidad persistida. El Id lógico PAC-2026-XXX se genera en la aplicación (no es IDENTITY en MySQL).</summary>
[Table("pacientes")]
public class Paciente
{
    [Key]
    [MaxLength(32)]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Column("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Range(1, 5)]
    [Column("gravedad")]
    public int Gravedad { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("estado")]
    public string Estado { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("carnet_medico")]
    public string CarnetMedico { get; set; } = string.Empty;

    [Column("fecha_registro")]
    public DateTime FechaRegistro { get; set; }
}
