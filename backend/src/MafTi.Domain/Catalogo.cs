namespace MafTi.Domain;

public class Catalogo
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty; 
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}