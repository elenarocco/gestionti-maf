namespace MafTi.Domain;

public class UsuarioSistema
{
    public int Id { get; set; }
    public string CorreoAD { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public int? AreaId { get; set; }
    public Catalogo? Area { get; set; }
    public bool Activo { get; set; } = true;
}