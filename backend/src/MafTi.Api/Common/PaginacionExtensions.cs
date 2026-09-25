using Microsoft.EntityFrameworkCore;

namespace MafTi.Api.Common;

public static class PaginacionExtensions
{
    public static async Task<PaginacionResultado<T>> PaginarAsync<T>(
        this IQueryable<T> query, int pagina, int tamanoPagina)
    {
        var total = await query.CountAsync();

        var datos = await query
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync();

        return new PaginacionResultado<T>
        {
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina,
            Datos = datos
        };
    }
}