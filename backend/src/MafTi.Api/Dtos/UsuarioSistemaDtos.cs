namespace MafTi.Api.Dtos;

public class UsuarioSistemaListaDto
{
    public int Id { get; set; }
    public int TrabajadorId { get; set; }
    public string TrabajadorNombre { get; set; } = string.Empty;
    public string TrabajadorCorreo { get; set; } = string.Empty;
    public string TrabajadorRut { get; set; } = string.Empty;
    public bool TrabajadorActivo { get; set; }
    public int RolId { get; set; }
    public string RolNombre { get; set; } = string.Empty;
    public int? AreaId { get; set; }
    public string? AreaNombre { get; set; }
    public bool Activo { get; set; }
    public bool EsUstedMismo { get; set; }   // el frontend no ofrece acciones sobre la propia cuenta
}

// Dar acceso a un trabajador.
public class UsuarioSistemaCreateDto
{
    public int TrabajadorId { get; set; }
    public int RolId { get; set; }
    public int? AreaId { get; set; }
}

// Cambiar rol y área de un acceso existente.
public class UsuarioSistemaCambioDto
{
    public int RolId { get; set; }
    public int? AreaId { get; set; }
}

// Trabajador activo que todavía no tiene acceso (para "Dar acceso").
public class CandidatoAccesoDto
{
    public int TrabajadorId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Rut { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public int AreaId { get; set; }
    public string AreaNombre { get; set; } = string.Empty;
}

public class CambioUsuarioDto
{
    public DateTime Fecha { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
    public string RealizadoPorNombre { get; set; } = string.Empty;
}
