using Microsoft.EntityFrameworkCore;
using Parcial2.Models;

namespace Parcial2.Data;

public class HospitalContext : DbContext
{
    public HospitalContext(DbContextOptions<HospitalContext> options)
        : base(options) { }

    public DbSet<Paciente> Pacientes => Set<Paciente>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Paciente>(e =>
        {
            e.ToTable("pacientes");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id)
                .ValueGeneratedNever();
        });
    }
}
