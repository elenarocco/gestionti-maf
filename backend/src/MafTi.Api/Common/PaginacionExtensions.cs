using Microsoft.EntityFrameworkCore;

namespace MafTi.Api.Common;

public static class PaginacionExtensions
{
    // Tope máximo de filas por página: evita que una sola petición vuelque toda la tabla (datos de personas)
    // y acota el costo de la consulta. Quien necesite más, pagina.
    public const int MaxTamanoPagina = 100;

    public static async Task<PaginacionResultado<T>> PaginarAsync<T>(
        this IQueryable<T> query, int pagina, int tamanoPagina)
    {
        // Se sanean los parámetros: una página <= 0 daría un OFFSET negativo (error 500) y un tamaño enorme
        // permitiría bajar todo de una vez.
        pagina = Math.Max(1, pagina);
        tamanoPagina = Math.Clamp(tamanoPagina, 1, MaxTamanoPagina);

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