namespace MafTi.Application;

// Estado de un acceso VPN según su fecha de término (en hora de Chile):
//   Vigente    sin fecha de término, o le quedan más de DiasAviso días.
//   PorVencer  le quedan DiasAviso días o menos (incluido el mismo día en que vence).
//   Vencido    la fecha de término ya pasó.
public static class ReglasVpn
{
    public const int DiasAviso = 15;
    public const string Vigente = "Vigente";
    public const string PorVencer = "PorVencer";
    public const string Vencido = "Vencido";

    public record Situacion(string Estado, int? DiasRestantes);

    public static Situacion Calcular(DateOnly? fechaHasta, DateOnly hoy)
    {
        if (fechaHasta is not DateOnly hasta) return new Situacion(Vigente, null);
        var dias = hasta.DayNumber - hoy.DayNumber;
        var estado = dias < 0 ? Vencido : dias <= DiasAviso ? PorVencer : Vigente;
        return new Situacion(estado, dias);
    }
}
