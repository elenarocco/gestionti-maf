using MafTi.Application;
using MafTi.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MafTi.Api.Common;

// Calcula el SLA de una solicitud con los feriados de la base (las reglas están en CalculadoraSla).
public class ServicioSla
{
    private readonly AppDbContext _context;

    public ServicioSla(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CalculadoraSla.Plazo> CalcularAsync(string tipoDb, DateTime creacionUtc)
    {
        // Ningún plazo pasa de unas semanas: basta con los feriados de los próximos 60 días.
        var hoy = DateOnly.FromDateTime(creacionUtc);
        var hasta = hoy.AddDays(60);
        var feriados = await _context.Feriados
            .Where(f => f.Fecha >= hoy.AddDays(-1) && f.Fecha <= hasta)
            .Select(f => f.Fecha)
            .ToListAsync();
        return CalculadoraSla.Calcular(tipoDb, creacionUtc, feriados.ToHashSet());
    }
}
