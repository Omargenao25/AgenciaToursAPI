namespace TourSmart.Web.Models;

public sealed class RespuestaLogin
{
    public string Token { get; set; } = "";
    public DateTime Expiracion { get; set; }
    public string Nombre { get; set; } = "";
    public string Rol { get; set; } = "";
}
