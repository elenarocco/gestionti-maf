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
    
    modelBuilder.Entity<Catalogo>()
    .HasOne(c => c.Padre)
    .WithMany()
    .HasForeignKey(c => c.PadreId)
    .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<SolicitudBloqueo>(e =>
{
    e.HasOne(x => x.Solicitud).WithOne()
        .HasForeignKey<SolicitudBloqueo>(x => x.SolicitudId)
        .OnDelete(DeleteBehavior.Restrict);
    e.HasIndex(x => x.SolicitudId).IsUnique();
    e.Property(x => x.Justificacion).HasMaxLength(200).IsRequired();
});

    modelBuilder.Entity<SolicitudModificacion>(e =>
{
    e.HasOne(x => x.Solicitud).WithOne()
        .HasForeignKey<SolicitudModificacion>(x => x.SolicitudId)
        .OnDelete(DeleteBehavior.Restrict);
    e.HasIndex(x => x.SolicitudId).IsUnique();
    e.Property(x => x.Justificacion).HasMaxLength(200).IsRequired();
});

    // TrabajadorId es opcional solo por VPN; se mantiene el borrado en cascada que ya tenía.
    modelBuilder.Entity<Solicitud>()
        .HasOne(s => s.Trabajador)
        .WithMany()
        .HasForeignKey(s => s.TrabajadorId)
        .IsRequired(false)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<SolicitudVpn>(e =>
{
    e.HasOne(x => x.Solicitud).WithOne()
        .HasForeignKey<SolicitudVpn>(x => x.SolicitudId)
        .OnDelete(DeleteBehavior.Restrict);
    e.HasIndex(x => x.SolicitudId).IsUnique();
    e.HasOne(x => x.Motivo).WithMany()
        .HasForeignKey(x => x.MotivoId)
        .OnDelete(DeleteBehavior.Restrict);
    e.Property(x => x.Rut).HasMaxLength(12);
    e.Property(x => x.Cargo).HasMaxLength(100);
    e.Property(x => x.Nombres).HasMaxLength(100);
    e.Property(x => x.Apellidos).HasMaxLength(100);
    e.Property(x => x.Departamento).HasMaxLength(100);
    e.Property(x => x.UbicacionFisica).HasMaxLength(150);
    e.Property(x => x.CompaniaExterna).HasMaxLength(150);
    e.Property(x => x.CuentaCorreo).HasMaxLength(150);
    e.Property(x => x.Justificacion).HasMaxLength(200);
});

    modelBuilder.Entity<SolicitudDetalle>()
        .Property(x => x.Accion).HasMaxLength(10).HasDefaultValue("Agregar");

    // Auditoría de descargas: borrar un usuario no debe borrar lo que descargó (se desactiva, no se borra).
    modelBuilder.Entity<RegistroDescarga>(e =>
    {
        e.HasOne(x => x.UsuarioSistema).WithMany().HasForeignKey(x => x.UsuarioSistemaId).OnDelete(DeleteBehavior.Restrict);
        e.Property(x => x.Reporte).HasMaxLength(50);
        e.Property(x => x.Formato).HasMaxLength(10);
        e.Property(x => x.Filtros).HasMaxLength(500);
        e.HasIndex(x => x.Fecha);
    });

    // Un solo acceso por trabajador: si se le quita, se desactiva y se puede reactivar.
    modelBuilder.Entity<UsuarioSistema>().HasIndex(x => x.TrabajadorId).IsUnique();

    // Bitácora de cambios de acceso: borrar un usuario no debe borrar su historial (se desactiva, no se borra).
    modelBuilder.Entity<CambioUsuarioSistema>(e =>
    {
        e.HasOne(x => x.UsuarioSistema).WithMany().HasForeignKey(x => x.UsuarioSistemaId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.RealizadoPor).WithMany().HasForeignKey(x => x.RealizadoPorId).OnDelete(DeleteBehavior.Restrict);
        e.Property(x => x.Accion).HasMaxLength(50);
        e.Property(x => x.Detalle).HasMaxLength(500);
    });
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
    public DbSet<SolicitudBloqueo> SolicitudesBloqueo => Set<SolicitudBloqueo>();
    public DbSet<SolicitudModificacion> SolicitudesModificacion => Set<SolicitudModificacion>();
    public DbSet<SolicitudVpn> SolicitudesVpn => Set<SolicitudVpn>();
    public DbSet<RegistroDescarga> RegistrosDescarga => Set<RegistroDescarga>();
    public DbSet<CambioUsuarioSistema> CambiosUsuarioSistema => Set<CambioUsuarioSistema>();
}