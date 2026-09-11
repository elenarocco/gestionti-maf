using Microsoft.EntityFrameworkCore;
using MafTi.Domain;

namespace MafTi.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Trabajador> Trabajadores { get; set; }
    public DbSet<Catalogo> Catalogos { get; set; }
    public DbSet<UsuarioSistema> UsuariosSistema { get; set; }
    public DbSet<Acceso> Accesos { get; set; }
    public DbSet<Solicitud> Solicitudes { get; set; }
}