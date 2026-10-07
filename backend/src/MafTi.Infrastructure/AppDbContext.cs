using Microsoft.EntityFrameworkCore;
using MafTi.Domain;

namespace MafTi.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Trabajador>()
        .HasOne(t => t.Area)
        .WithMany()
        .HasForeignKey(t => t.AreaId);

    modelBuilder.Entity<Trabajador>()
        .HasOne(t => t.DireccionCorporativa)
        .WithMany()
        .HasForeignKey(t => t.DireccionCorporativaId);

    modelBuilder.Entity<Trabajador>()
        .HasOne(t => t.LugarTrabajo)
        .WithMany()
        .HasForeignKey(t => t.LugarTrabajoId);
        
    modelBuilder.Entity<Trabajador>()
    .HasOne(t => t.Cargo)
    .WithMany()
    .HasForeignKey(t => t.CargoId)
    .OnDelete(DeleteBehavior.Restrict);
}

    public DbSet<Trabajador> Trabajadores { get; set; }
    public DbSet<Catalogo> Catalogos { get; set; }
    public DbSet<UsuarioSistema> UsuariosSistema { get; set; }
    public DbSet<Acceso> Accesos { get; set; }
    public DbSet<Solicitud> Solicitudes { get; set; }
    public DbSet<SolicitudDetalle> SolicitudDetalles { get; set; }
    public DbSet<HistorialSolicitud> HistorialSolicitudes { get; set; }
    public DbSet<Rol> Roles { get; set; }
    public DbSet<Permiso> Permisos { get; set; }
    public DbSet<RolPermiso> RolPermisos { get; set; }
}