namespace MafTi.Domain;

// Feriado nacional de Chile: no cuenta como día hábil para el SLA de las solicitudes.
public class Feriado
{
    public int Id { get; set; }
    public DateOnly Fecha { get; set; }
    public string Nombre { get; set; } = string.Empty;
}
