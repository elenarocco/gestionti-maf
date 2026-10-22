using MafTi.Domain;
using MafTi.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MafTi.Api.Common;

// "Campo: antes → después" por cada dato que cambia en una Modificación (los catálogos, por nombre).
// Se guarda en el comentario del paso "Creada" del historial, así queda aunque luego se aplique el cambio.
// Adaptado de 5b981a2 (feature/ariel).
public static class CambiosModificacion
{
    public static async Task<List<string>> DescribirAsync(AppDbContext db, Trabajador antes, Trabajador despues)
    {
        var ids = new[] { antes.DireccionCorporativaId, antes.AreaId, antes.CargoId, antes.LugarTrabajoId,
                          despues.DireccionCorporativaId, despues.AreaId, despues.CargoId, despues.LugarTrabajoId };
        var nombres = await db.Catalogos.Where(c => ids.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Nombre);
        string Cat(int id) => nombres.GetValueOrDefault(id, id.ToString());
        static string Fecha(DateOnly f) => f.ToString("dd-MM-yyyy");
        static string SiNo(bool b) => b ? "Sí" : "No";

        var cambios = new List<string>();
        void Comparar(string campo, string? a, string? b, bool ignorarMayusculas = false)
        {
            var x = (a ?? "").Trim();
            var y = (b ?? "").Trim();
            if (!string.Equals(x, y, ignorarMayusculas ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                cambios.Add($"{campo}: {(x == "" ? "—" : x)} → {(y == "" ? "—" : y)}");
        }

        Comparar("Primer nombre", antes.PrimerNombre, despues.PrimerNombre);
        Comparar("Segundo nombre", antes.SegundoNombre, despues.SegundoNombre);
        Comparar("Primer apellido", antes.PrimerApellido, despues.PrimerApellido);
        Comparar("Segundo apellido", antes.SegundoApellido, despues.SegundoApellido);
        Comparar("Fecha de nacimiento", Fecha(antes.FechaNacimiento), Fecha(despues.FechaNacimiento));
        Comparar("Sexo", antes.Sexo, despues.Sexo);
        Comparar("Correo", antes.Correo, despues.Correo, ignorarMayusculas: true);
        Comparar("Dirección corporativa", Cat(antes.DireccionCorporativaId), Cat(despues.DireccionCorporativaId));
        Comparar("Área", Cat(antes.AreaId), Cat(despues.AreaId));
        Comparar("Cargo", Cat(antes.CargoId), Cat(despues.CargoId));
        Comparar("Lugar de trabajo", Cat(antes.LugarTrabajoId), Cat(despues.LugarTrabajoId));
        Comparar("Fecha de incorporación", Fecha(antes.FechaIncorporacion), Fecha(despues.FechaIncorporacion));
        Comparar("Dirección de domicilio", antes.DireccionDomicilio, despues.DireccionDomicilio);
        Comparar("Jefe directo", antes.JefeDirecto, despues.JefeDirecto);
        Comparar("Homologar accesos desde", antes.HomologarAccesosDesde, despues.HomologarAccesosDesde);
        Comparar("Teléfono corporativo", SiNo(antes.TieneTelefonoCorporativo), SiNo(despues.TieneTelefonoCorporativo));
        Comparar("Solicita teléfono", SiNo(antes.SolicitaTelefono), SiNo(despues.SolicitaTelefono));
        return cambios;
    }

    // Los cambios guardados al crear la solicitud. Las Modificaciones anteriores a esto no los tienen:
    // si aún no se completan, se calculan contra los datos actuales del trabajador.
    public static async Task<List<string>> DeSolicitudAsync(AppDbContext db, Solicitud s)
    {
        var comentarioCreada = await db.HistorialSolicitudes
            .Where(h => h.SolicitudId == s.Id && h.Accion == "Creada")
            .Select(h => h.Comentario)
            .FirstOrDefaultAsync();
        var cambios = (comentarioCreada ?? "").Split('\n').Where(l => l.Contains(" → ")).ToList();
        if (cambios.Count > 0 || s.Estado == "Completada" || s.Trabajador == null) return cambios;

        var m = await db.SolicitudesModificacion.FirstOrDefaultAsync(x => x.SolicitudId == s.Id);
        return m == null ? cambios : await DescribirAsync(db, s.Trabajador, PropuestoDesde(m));
    }

    public static Trabajador PropuestoDesde(SolicitudModificacion m) => new()
    {
        PrimerNombre = m.PrimerNombre, SegundoNombre = m.SegundoNombre,
        PrimerApellido = m.PrimerApellido, SegundoApellido = m.SegundoApellido,
        FechaNacimiento = m.FechaNacimiento, Sexo = m.Sexo, Correo = m.Correo,
        DireccionCorporativaId = m.DireccionCorporativaId, AreaId = m.AreaId, CargoId = m.CargoId,
        LugarTrabajoId = m.LugarTrabajoId, FechaIncorporacion = m.FechaIncorporacion,
        DireccionDomicilio = m.DireccionDomicilio, JefeDirecto = m.JefeDirecto,
        HomologarAccesosDesde = m.HomologarAccesosDesde,
        TieneTelefonoCorporativo = m.TieneTelefonoCorporativo, SolicitaTelefono = m.SolicitaTelefono
    };

    public static string? UnirLineas(IEnumerable<string?> lineas)
    {
        var texto = string.Join("\n", lineas.Where(l => !string.IsNullOrWhiteSpace(l)));
        return texto == "" ? null : texto;
    }
}
